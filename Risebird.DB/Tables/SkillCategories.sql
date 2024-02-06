CREATE TABLE SkillCategories (
    Id int Identity Primary Key,
    Name varchar(50),
	ImageURL varchar(500),
	IsActive bit default 1
);