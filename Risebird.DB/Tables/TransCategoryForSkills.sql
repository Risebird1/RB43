CREATE TABLE TransCategoryForSkills (
    Id int Identity,
    SkillId int,
	CatgoryId int,
	FOREIGN KEY (CatgoryId) REFERENCES SkillCategories(Id),
	FOREIGN KEY (SkillId) REFERENCES Skills(Id)
);
