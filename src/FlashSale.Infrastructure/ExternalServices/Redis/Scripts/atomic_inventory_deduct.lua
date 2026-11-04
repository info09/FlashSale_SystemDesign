-- Lua Script: Atomic Inventory Offloading (nguyên tử, không chia thành nhiều lệnh)
-- KEYS[1]: key lưu số lượng tồn kho trong Redis (vi du: inventory:{productId})
-- ARGV[1]: quantityToDeduct (so luong can tru)

local key = KEYS[1]
local quantityToDeduct = tonumber(ARGV[1])

-- Lay so luong ton kho hien tai
local current = tonumber(redis.call("GET", key) or "0")

-- Neu khong du kho, tra ve nil (signal Co thue)
if current < quantityToDeduct then
    return nil
end

-- Tru kho atomic
local newQuantity = current - quantityToDeduct
redis.call("SET", key, newQuantity)

-- Tra ve so luong con lai
return newQuantity
