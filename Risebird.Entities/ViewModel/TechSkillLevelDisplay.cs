using System;

namespace Risebird.Entities.ViewModel
{
    /// <summary>
    /// Values of IRF.LevelDisplayMode. Null and <see cref="Legacy"/> both mean today's single
    /// "Evaluated at" line, so no channel changes until the column is set.
    /// </summary>
    public static class LevelDisplayModes
    {
        public const int Legacy = 0;
        /// <summary>Required and actual level as text, plus the competency scale section.</summary>
        public const int LevelsAsText = 1;
        /// <summary>As <see cref="LevelsAsText"/>, plus the five-star attainment row.</summary>
        public const int LevelsWithStars = 2;

        public static bool IsEnhanced(int? mode) => mode == LevelsAsText || mode == LevelsWithStars;
    }

    /// <summary>
    /// What one skill row of the client report shows when IRF.LevelDisplayMode is set. Display only:
    /// the stars are a re-rendering of the TechSkill_Score the view already computes, never a new
    /// calculation, so the stars and the bar beside them can never disagree.
    /// </summary>
    public class TechSkillLevelDisplay
    {
        // LevelTech ids. Required is 2-6; actual is 1-7.
        public const int CluelessLevelId = 1;
        public const int NotTestedLevelId = 7;

        public int Mode { get; private set; }
        public string RequiredLevelName { get; private set; }
        public string ActualLevelName { get; private set; }
        public int ActualLevelId { get; private set; }

        /// <summary>
        /// False when the report's existing score gate is closed (a client profile that has not yet
        /// accepted a Reportsnippet = 1 report). The verdict, the actual level and the stars all reveal
        /// the rating, so they are withheld and a neutral locked state is shown instead.
        /// </summary>
        public bool ScoreVisible { get; private set; }

        /// <summary>0-5, from TechSkill_Score. Meaningless when <see cref="IsNotTested"/> or not visible.</summary>
        public int Stars { get; private set; }

        public bool ShowStars => Mode == LevelDisplayModes.LevelsWithStars;
        public bool IsNotTested => ActualLevelId == NotTestedLevelId;
        public bool IsNotDemonstrated => ActualLevelId == CluelessLevelId;

        public static TechSkillLevelDisplay Create(int? mode, TechSkillReport skill, double techSkillScore, bool scoreVisible)
        {
            var stars = (int)Math.Round(techSkillScore / 20);
            return new TechSkillLevelDisplay
            {
                Mode = mode ?? LevelDisplayModes.Legacy,
                RequiredLevelName = ClientLevelName(skill?.RequiredLevelId ?? 0, skill?.RequiredLevel),
                ActualLevelName = ClientLevelName(skill?.ActualLevelId ?? 0, skill?.ActualLevel),
                ActualLevelId = skill?.ActualLevelId ?? 0,
                ScoreVisible = scoreVisible,
                Stars = Math.Max(0, Math.Min(5, stars))
            };
        }

        /// <summary>
        /// Client-facing level name, by id, so the report is right whatever LevelTech.Name holds in a given
        /// environment. Id 3 is Dreyfus "Advanced Beginner" (Tables\IRF.sql renames the stored row to match);
        /// id 1 ("Clueless") is shown to clients as "Not demonstrated".
        /// </summary>
        public static string ClientLevelName(int levelId, string storedName)
        {
            switch (levelId)
            {
                case 1: return "Not demonstrated";
                case 2: return "Novice";
                case 3: return "Advanced Beginner";
                case 4: return "Competent";
                case 5: return "Proficient";
                case 6: return "Expert";
                case 7: return "Not tested";
                default: return storedName ?? string.Empty;
            }
        }

        /// <summary>Verdict chip text for the skill header line.</summary>
        public string Verdict
        {
            get
            {
                if (IsNotTested) return "Not tested";
                if (IsNotDemonstrated) return "Not demonstrated";
                switch (Stars)
                {
                    case 5: return "Meets required";
                    case 4: return "One level below";
                    case 3: return "Two levels below";
                    case 2: return "Three levels below";
                    case 1: return "Four levels below";
                    default: return "Not demonstrated";
                }
            }
        }

        /// <summary>Text and background colour of the verdict chip.</summary>
        public string VerdictColor => IsNotTested ? "#6b7280" : Stars >= 5 ? "#15803d" : (IsNotDemonstrated || Stars == 0) ? "#b91c1c" : "#b45309";
        public string VerdictBackground => IsNotTested ? "#f3f4f6" : Stars >= 5 ? "#dcfce7" : (IsNotDemonstrated || Stars == 0) ? "#fee2e2" : "#fef3c7";
    }
}
