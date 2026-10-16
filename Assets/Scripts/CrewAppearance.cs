namespace ScammerTown
{
    // Saved, one-based IDs; zero is an employee from a save predating crew looks.
    public static class CrewAppearance
    {
        public const int Count=4;
        public static string Atlas(int appearance)
        {
            switch(appearance)
            {
                case 2: return "scam_crew_vest";
                case 3: return "scam_crew_beanie";
                case 4: return "scam_crew_leather";
                default: return "scam_crew";
            }
        }
    }
}
