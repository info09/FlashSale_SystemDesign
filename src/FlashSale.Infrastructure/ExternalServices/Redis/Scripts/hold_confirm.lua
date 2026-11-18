-- KEYS[1] = hold key (hold:{holdId})
-- KEYS[2] = user hold index key (hold:user:{clientId})
-- KEYS[3] = holds expiring zset key (holds:expiring)
-- ARGV[1] = holdId
-- ARGV[2] = clientId
-- ARGV[3] = nowEpochMs

local holdKey = KEYS[1]
local userKey = KEYS[2]
local expiringKey = KEYS[3]

local holdId = ARGV[1]
local clientId = ARGV[2]
local nowEpochMs = tonumber(ARGV[3])

if redis.call('EXISTS', holdKey) == 0 then
    return 'NOT_FOUND'
end

local status = redis.call('HGET', holdKey, 'status')
local owner = redis.call('HGET', holdKey, 'clientId')
local expiresAt = tonumber(redis.call('HGET', holdKey, 'expiresAt') or '0')
local productId = redis.call('HGET', holdKey, 'productId') or ''
local quantity = redis.call('HGET', holdKey, 'quantity') or '0'

if owner ~= clientId then
    return 'NOT_OWNED'
end

if status == 'consumed' then
    return 'CONSUMED'
end

if status == 'released' or status == 'expired' then
    return 'INVALID'
end

if expiresAt > 0 and nowEpochMs >= expiresAt then
    return 'EXPIRED'
end

-- consume hold
redis.call('HSET', holdKey, 'status', 'consumed')
redis.call('ZREM', expiringKey, holdId)

local indexedHoldId = redis.call('GET', userKey)
if indexedHoldId == holdId then
    redis.call('DEL', userKey)
end

return 'OK,' .. productId .. ',' .. quantity .. ',' .. owner .. ',' .. tostring(expiresAt)
