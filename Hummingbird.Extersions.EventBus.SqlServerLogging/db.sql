
CREATE TABLE EventLogs
(
	EventId bigint not null  PRIMARY KEY,
	MessageId varchar(50) not null,
	Content NVARCHAR(max),
	CreationTime DATETIME2 NOT NULL,
	EventTypeName  NVARCHAR(500),
	State INT NOT NULL,
	TimesSent INT NOT NULL
)