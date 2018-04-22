SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [Openchain].[GetTransactionByRecordKeys1]
    @instance INT,
    @ids [Openchain].[IdTable] READONLY,
	@start datetime,
	@end datetime
AS
    SET NOCOUNT ON;

SELECT        Openchain.Transactions.RawData
FROM            Openchain.RecordMutations
			INNER JOIN @ids AS Ids ON Openchain.RecordMutations.RecordKey = Ids.[Id]
			INNER JOIN Openchain.Transactions ON Openchain.RecordMutations.TransactionId = Openchain.Transactions.Id
WHERE        (Openchain.RecordMutations.Instance = @instance) AND Created between @start and @end

RETURN;
