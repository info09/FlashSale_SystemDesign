-- KEYS[1] = inventory key (inventory:{productId})
-- KEYS[2] = hold key (hold:{holdId})
-- KEYS[3] = user hold index key (hold:user:{clientId})
-- KEYS[4] = holds expiring zset key (holds:expiring)
-- ARGV[1] = quantity
-- ARGV[2] = clientId
-- ARGV[3] = holdId
-- ARGV[4] = ttlSeconds
-- ARGV[5] = expiresAtEpochMs
-- ARGV[6] = productId

local inventoryKey = KEYS[1]
local holdKey = KEYS[2]
local userKey = KEYS[3]
local expiringKey = KEYS[4]

local quantity = tonumber(ARGV[1])
local clientId = ARGV[2]
local holdId = ARGV[3]
local ttlSeconds = tonumber(ARGV[4])
local expiresAtEpochMs = ARGV[5]
local productId = ARGV[6]

-- 1 user / 1 active hold
local existingHoldId = redis.call('GET', userKey)
if existingHoldId and existingHoldId ~= false then
    local existingHoldKey = 'hold:' .. existingHoldId
    if redis.call('EXISTS', existingHoldKey) == 1 then
        local status = redis.call('HGET', existingHoldKey, 'status')
        local owner = redis.call('HGET', existingHoldKey, 'clientId')
        if status == 'active' and owner == clientId then
            return 'ALREADY_EXISTS,' .. existingHoldId
        end
    end
    -- stale index cleanup
    redis.call('DEL', userKey)
    redis.call('ZREM', expiringKey, existingHoldId)
end

-- check inventory
local current = tonumber(redis.call('GET', inventoryKey) or '0')
if current < quantity then
    return 'INSUFFICIENT,' .. tostring(current)
end

-- reserve inventory
redis.call('DECRBY', inventoryKey, quantity)

-- create hold
redis.call('HSET', holdKey,
    'holdId', holdId,
    'productId', productId,
    'quantity', tostring(quantity),
    'clientId', clientId,
    'status', 'active',
    'expiresAt', expiresAtEpochMs
)
redis.call('EXPIRE', holdKey, ttlSeconds)

-- user index + expiry index
redis.call('SET', userKey, holdId, 'EX', ttlSeconds)
redis.call('ZADD', expiringKey, tonumber(expiresAtEpochMs), holdId)

return 'OK,' .. holdId .. ',' .. expiresAtEpochMs .. ',' .. tostring(ttlSeconds)
