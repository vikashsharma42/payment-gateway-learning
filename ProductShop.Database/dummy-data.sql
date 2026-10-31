--SET IDENTITY_INSERT Products ON;

--INSERT INTO Products (Id, Name, Description, Price, ImageUrl)
--SELECT v.Id, v.Name, v.Description, v.Price, v.ImageUrl
--FROM (VALUES
--    (1,  N'Laptop',              N'14-inch, 16GB RAM, 512GB SSD',   65999.00, N'https://picsum.photos/seed/laptop/600/400'),
--    (2,  N'Smartphone',          N'6.5-inch AMOLED, 128GB storage', 24999.00, N'https://picsum.photos/seed/phone/600/400'),
--    (3,  N'Wireless Headphones', N'Noise cancelling, 30h battery',  2999.00,  N'https://picsum.photos/seed/headphones/600/400'),
--    (4,  N'Smartwatch',          N'Fitness tracking, heart rate',   4499.00,  N'https://picsum.photos/seed/watch/600/400'),
--    (5,  N'Mechanical Keyboard', N'RGB backlit, blue switches',     1499.00,  N'https://picsum.photos/seed/keyboard/600/400'),
--    (6,  N'Wireless Mouse',      N'Ergonomic, 2.4GHz receiver',     799.00,   N'https://picsum.photos/seed/mouse/600/400'),
--    (7,  N'27" Monitor',         N'Full HD, 75Hz IPS panel',        12999.00, N'https://picsum.photos/seed/monitor/600/400'),
--    (8,  N'Tablet',              N'10.9-inch, 64GB, Wi-Fi',         18999.00, N'https://picsum.photos/seed/tablet/600/400'),
--    (9,  N'Backpack',            N'Water resistant, 20L capacity',  1299.00,  N'https://picsum.photos/seed/backpack/600/400'),
--    (10, N'Water Bottle',        N'Steel, 1L, keeps cold 24h',      499.00,   N'https://picsum.photos/seed/bottle/600/400')
--) AS v (Id, Name, Description, Price, ImageUrl)
--WHERE NOT EXISTS (SELECT 1 FROM Products p WHERE p.Id = v.Id);

--SET IDENTITY_INSERT Products OFF;
--GO