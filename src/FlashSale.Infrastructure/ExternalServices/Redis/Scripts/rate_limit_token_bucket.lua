-- Lua Script: Token Bucket Rate Limit (nguyên tử, không chia thành nhiều lệnh)
-- KEYS[1]: key lưu trạng thái token bucket (ví dụ: rate_limit:{clientId})
-- ARGV[1]: maxRequests (số token tối đa trong 1 cửa sổ)
-- ARGV[2]: windowSeconds (khoảng thời gian reset, giây)
-- ARGV[3]: now (timestamp Unix epoch, giây)

local key = KEYS[1]
local maxRequests = tonumber(ARGV[1])
local windowSeconds = tonumber(ARGV[2])
local now = tonumber(ARGV[3])

-- Lấy trạng thái hiện tại từ Redis (format: tokens:last_refill)
local current = redis.call("GET", key)
local tokens = maxRequests
local lastRefill = now

if current then
    local parts = {}
    for part in string.gmatch(current, "([^:]+)") do
        table.insert(parts, part)
    end
    tokens = tonumber(parts[1]) or maxRequests
    lastRefill = tonumber(parts[2]) or now
end

-- Tính toán số token đã nạp thêm từ lần cuối cùng đến hiện tại
local elapsed = now - lastRefill
if elapsed > 0 then
    local refill = math.floor(elapsed * maxRequests / windowSeconds)
    if refill > 0 then
        tokens = math.min(maxRequests, tokens + refill)
        lastRefill = now
    end
end

local allowed = 0
local resetAfter = windowSeconds

if tokens >= 1 then
    -- Có token, tiêu 1 token
    tokens = tokens - 1
    allowed = 1
    resetAfter = 0
else
    -- Không còn token, tính thời gian chờ đến khi có token mới
    local elapsedSinceLastRefill = now - lastRefill
    resetAfter = math.max(0, windowSeconds - (elapsedSinceLastRefill % windowSeconds))
end

-- Lưu trạng thái mới vào Redis (format: tokens:last_refill)
redis.call("SET", key, tokens .. ":" .. lastRefill)
redis.call("EXPIRE", key, windowSeconds * 2)

return allowed .. "," .. tokens .. "," .. resetAfter
