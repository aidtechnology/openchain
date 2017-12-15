CREATE PROCEDURE [Openchain].[GetTransactionByRecordKeys]
    @instance INT,
    @ids [Openchain].[IdTable] READONLY
AS
    SET NOCOUNT ON;

SELECT        Openchain.Transactions.RawData
FROM            Openchain.RecordMutations
			INNER JOIN @ids AS Ids ON Openchain.RecordMutations.RecordKey = Ids.[Id]
			INNER JOIN Openchain.Transactions ON Openchain.RecordMutations.TransactionId = Openchain.Transactions.Id
WHERE        (Openchain.RecordMutations.Instance = @instance)

RETURN;