/*------------ DROPY
DROP TABLE User;
*/

CREATE DATABASE DNA
GO

USE DNA
CREATE TABLE "User"
(
	"Id" INT IDENTITY(1,1) PRIMARY KEY,
	"Login" VARCHAR(30) NOT NULL UNIQUE,
	"Password" VARCHAR(30) NOT NULL
);

CREATE TABLE "Friend"
(
	"Id" INT IDENTITY(1,1) PRIMARY KEY,
	"OwnerId" int FOREIGN KEY REFERENCES "User"(Id),
	"FriendId" int FOREIGN KEY REFERENCES "User"(Id),
	"FriendLabel" VARCHAR(30) NOT NULL
);
