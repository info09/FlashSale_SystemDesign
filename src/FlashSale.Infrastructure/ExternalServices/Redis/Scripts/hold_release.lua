-- KEYS[1] = hold key (hold:{holdId})
-- KEYS[2] = user hold index key (hold:user:{clientId}) (may be placeholder for system release)
-- KEYS[3] = holds expiring zset key (holds:expiring)
-- ARGV[1] = holdId
-- ARGV[2] = clientId (optional, empty for system/worker release)
-- ARGV[3] = mode: "user" | "expire" | "force"

local holdKey = KEYS[1]
local userKey = KEYS[2]
local expiringKey = KEYS[3]

local holdId = ARGV[1]
local clientId = ARGV[2] or ''
local mode = ARGV[3] or 'user'

if redis.call('EXISTS', holdKey) == 0 then
    -- idempotent: if already removed from indexes, treat as released
    redis.call('ZREM', expiringKey, holdId)
    if clientId ~= '' then
        local indexedHoldId = redis.call('GET', userKey)
        if indexedHoldId == holdId then
            redis.call('DEL', userKey)
        end
    end
    return 'NOT_FOUND'
end

local status = redis.call('HGET', holdKey, 'status')
local owner = redis.call('HGET', holdKey, 'clientId') or ''
local quantity = tonumber(redis.call('HGET', holdKey, 'quantity') or '0')

if mode == 'user' and clientId ~= '' and owner ~= clientId then
    return 'NOT_OWNED'
end

if status == 'consumed' then
    redis.call('ZREM', expiringKey, holdId)
    if owner ~= '' then
        local ownerUserKey = 'hold:user:' .. owner
        local indexedHoldId = redis.call('GET', ownerUserKey)
        if indexedHoldId == holdId then
            redis.call('DEL', ownerUserKey)
        end
    end
    return 'CONSUMED'
end

if status == 'released' or status == 'expired' then
    redis.call('ZREM', expiringKey, holdId)
    if owner ~= '' then
        local ownerUserKey = 'hold:user:' .. owner
        local indexedHoldId = redis.call('GET', ownerUserKey)
        if indexedHoldId == holdId then
            redis.call('DEL', ownerUserKey)
        end
    end
    if status == 'expired' then
        return 'EXPIRED_RELEASED'
    end
    return 'ALREADY_RELEASED'
end

-- active -> release and return inventory
local productId = redis.call('HGET', holdKey, 'productId') or ''
if quantity > 0 and productId ~= '' then
    redis.call('INCRBY', 'inventory:' .. productId, quantity)
end

if mode == 'expire' then
    redis.call('HSET', holdKey, 'status', 'expired')
else
    redis.call('HSET', holdKey, 'status', 'released')
end

redis.call('ZREM', expiringKey, holdId)

if owner ~= '' then
    local ownerUserKey = 'hold:user:' .. owner
    local indexedHoldId = redis.call('GET', ownerUserKey)
    if indexedHoldId == holdId then
        redis.call('DEL', ownerUserKey)
    end
end

-- keep hold record briefly for idempotency/debug; worker/user can re-run safely via status
redis.call('EXPIRE', holdKey, 300)

if mode == 'expire' then
    return 'EXPIRED_RELEASED'
end
return 'RELEASED'
