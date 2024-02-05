CREATE TABLE Skills (
    Id int Identity Primary Key,
    Name varchar(50),
	ImageURL varchar(500),
	IsPrimary bit default 0,
	IsActive bit default 1
);