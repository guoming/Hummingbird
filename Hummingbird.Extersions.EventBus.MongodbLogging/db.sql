
CREATE TABLE EventLogs
(
	EventId bigint not null  PRIMARY KEY,
	Content NVARCHAR(max),
	CreationTime DATETIME2 NOT NULL,
	EventTypeName  NVARCHAR(500),
	State INT NOT NULL,
	TimesSent INT NOT NULL
)

CREATE TABLE EventRecivedLogs
(	
	EventId bigint NOT NULL PRIMARY KEY,
	QueueName NVARCHAR(500) NOT NULL PRIMARY KEY,
	CreationTime DATETIME2 NOT NULL,
)

CREATE TABLE EventFailedLogs
(	
	EventId bigint NOT NULL ,
	QueueName NVARCHAR(500) NOT NULL,
	CreationTime DATETIME2 NOT NULL,
)