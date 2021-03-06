﻿DECLARE @CurrentMigration [nvarchar](max)

IF object_id('[dbo].[__MigrationHistory]') IS NOT NULL
    SELECT @CurrentMigration =
        (SELECT TOP (1) 
        [Project1].[MigrationId] AS [MigrationId]
        FROM ( SELECT 
        [Extent1].[MigrationId] AS [MigrationId]
        FROM [dbo].[__MigrationHistory] AS [Extent1]
        WHERE [Extent1].[ContextKey] = N'TestEf.Migrations.Configuration'
        )  AS [Project1]
        ORDER BY [Project1].[MigrationId] DESC)

IF @CurrentMigration IS NULL
    SET @CurrentMigration = '0'

IF @CurrentMigration < '201706191949170_AutomaticMigration'
BEGIN
    CREATE TABLE [dbo].[Lectures] (
        [Id] [int] NOT NULL IDENTITY,
        [LectureName] [nvarchar](max),
        CONSTRAINT [PK_dbo.Lectures] PRIMARY KEY ([Id])
    )
    CREATE TABLE [dbo].[Students] (
        [Id] [int] NOT NULL IDENTITY,
        [Name] [nvarchar](max),
        [Lecture_Id] [int],
        CONSTRAINT [PK_dbo.Students] PRIMARY KEY ([Id])
    )
    CREATE INDEX [IX_Lecture_Id] ON [dbo].[Students]([Lecture_Id])
    ALTER TABLE [dbo].[Students] ADD CONSTRAINT [FK_dbo.Students_dbo.Lectures_Lecture_Id] FOREIGN KEY ([Lecture_Id]) REFERENCES [dbo].[Lectures] ([Id])
    CREATE TABLE [dbo].[__MigrationHistory] (
        [MigrationId] [nvarchar](150) NOT NULL,
        [ContextKey] [nvarchar](300) NOT NULL,
        [Model] [varbinary](max) NOT NULL,
        [ProductVersion] [nvarchar](32) NOT NULL,
        CONSTRAINT [PK_dbo.__MigrationHistory] PRIMARY KEY ([MigrationId], [ContextKey])
    )
    INSERT [dbo].[__MigrationHistory]([MigrationId], [ContextKey], [Model], [ProductVersion])
    VALUES (N'201706191949170_AutomaticMigration', N'TestEf.Migrations.Configuration',  0x1F8B0800000000000400ED59DB6EE336107D2FD07F10F4D416592B9797D6B076913A4961343744C9A26F012D8D1DA214A98A5460A3E897F5A19FD45FE8507791962DBB9B6D51147989C4E1F0CCCCE15CE43F7FFF63F2611533E715524905F7DD93D1B1EB000F4544F9D27733B578F7ADFBE1FD975F4C2EA378E57CACE4CEB41CEEE4D2775F944AC69E27C31788891CC5344C85140B350A45EC914878A7C7C7DF7927271EA00A177539CEE421E38AC6903FE0E354F01012951176232260B27C8F2B41AED5B92531C88484E0BB8F20D5E5C275CE1925787A000C1F08E7421185D8C64F1202950ABE0C127C41D8E33A01945B1026A1C43C6EC487C23F3ED5F0BD6663A52ACCA412F19E0A4FCE4A7F78E6F683BCEAD6FE428F5DA267D55A5B9D7BCD77AF2154598AB69B678DA72CD572BE0B8B8491F5A8943C728AE7A33ADE480BFD77E44C33A6257C0E994A093B72EEB339A3E18FB07E143F03F779C6581B0DE2C1B5CE0B7C759F8A0452B57E8045897116B98ED7DDE7991BEB6DAD3D05FC195767A7AE738B879339833AD82D53032552F80138A44441744F9482946B1D90BBCB3ADD38ABF48C7EA80E4592E11D719D1BB2BA06BE542FBE8BFFBACE155D4154BD29813C718A570A37A93403EBAC5BF24A97394CE3D440651A9F749D0760B9807CA149C1F92A58CF8DD0552AE207C11AB8F5DA7320B234D4D0458FC0234997A0BAD0265EC3A5AD0C2BB50C605829F93FC3ACB3DE805A7DF13B9752843447DDA577C3A52ED44B1E39BB8855006F1113E163286982C1430CBE7B3C1A9D584ED8A2B962644B73CDB3AEE66F4CB35B06DABCC55AA308C53095B6EB67586D222F569292BFB274AF095EEB0B4075BD8837B1F1BBE913CBFEAE8AE6325B2A6AE30D152D632D28ADE4D012EB4B20E6251A10F61A7FDB7A6F0F4555945B8A1ACCE67DED9ABA81E9756C9B06C22B3A88AAD3F07A5A8DC90D4912BC74ADD6A37CE30445DF317D17EC5F9CE3428717CA0D35BA465B9F8429842CC158D5148DE08AA6525D1045E6445FFB69146F102B98DC43B1EA1483AC76B02AE2551BF4FFC5A6A2F7AAD96C6C6D7C768566C418C3DC22B04962EFCC5B3DC248BA21054F05CB62DE97C6B7EDEE94EDB69ACE82AD6FE21986984EF22C2F195435BD3E28265B6EE2CE9854E961FF98F4EE7C9B98D8C1F8A7A2D04D271BAF472B81F6DD8296C840CFEA7CB899A85682B57D32282295BA4D91D15EA98F3F005999B10F44B637224C6A11CD5B9599D4ED57DDF80CB3D72C1A3611ACDA618AD434AC6B88512B2665DEDE3DBB5A89BC10711DB4FE95463A89076BA9201E698151F00B9B329A17FD4AE08670BA40FE15CDB08BA3E4A9310AFF7BC6524FCA880D9B4D3F7B434FB55377B6EC56BFBFF794C85F491ABE90F4AB98ACBE6E2B1CDEAEF78E5BFF0DA71DE8AD5EEF3F1B78ADB67EC62358F9EEAFF9B6B133FBE9B9D979E4DCA578CBC6CEB1F3DB61D1FAFBC3D52718A6EA5961EFE1497F9B8305A43A9884614E94388553BBF4DEE3841AD2843003BB5D16869050BBB3D668AE5C40025C43378C1B72D4F642586B366EC42E0FEC3D619AEDFBA03972DB1859D40DDF8DE602835C70B46F00EB1B32B7CD989BF4F7CE659F7102DD34716E1D380D5B3B6DD99BCC97762F807C6A7DED463A4BBA6C54E80687A3216D26D53233BE1015A90D44958891046F40910869769E2ABA20A1C2E510A4CC3F277D242C4391CB780ED18CDF652AC9149A0CF19C753E4FE98BB1EDFC7C88EE629EDC25F9E7C94F6102C2A46802DCF1EF33CAA21AF7955D4DFA54E81B5796281D4BA54BD5725D6BBA157CA0A2D27D75A278843861A84CDEF180BCC221D89E245CC39284EBAAA5EB57B23B105DB74F2E2859A62496A58E66BFFE05C7D33FE1BCFF0B1422F729F4190000 , N'6.1.3-40302')
END

