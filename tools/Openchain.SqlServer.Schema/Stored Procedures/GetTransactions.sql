SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [Openchain].[GetTransactions]
    @instance INT,
	@mutationHashes [Openchain].[IdTable] READONLY
AS
    SET NOCOUNT ON;

    SELECT [RawData]
    FROM [Openchain].[Transactions]
	INNER JOIN @mutationHashes AS Hashes ON Transactions.[MutationHash] = Hashes.[Id]
    WHERE Transactions.[Instance] = @instance

RETURN;
