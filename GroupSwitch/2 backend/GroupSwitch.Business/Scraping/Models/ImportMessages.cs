namespace GroupSwitch.Business.Scraping.Models
{
    public static class ImportMessages
    {
        public const string Started = "Import démarré";
        public const string GroupSavedFormat = "{0} : {1} séances, {2} semaines";
        public const string WeekRejectedFormat = "{0}, semaine {1} non enregistrée ({2}) : {3}";
        public const string FinishedFormat = "Import terminé ({0}) : {1} groupes, {2} séances, {3} semaines enregistrées, {4} semaines rejetées";
        public const string Cancelled = "Import annulé à la demande.";
        public const string Shutdown = "Import interrompu : arrêt de l'application.";
        public const string Interrupted = "Import interrompu : l'application a redémarré pendant l'import.";
        public const string TooManyFailedGroupsFormat = "Import arrêté : {0} groupes de suite sans aucune semaine récupérée (site indisponible ou page dans un état inattendu).";
        public const string UnexpectedFailureFormat = "Import arrêté sur une erreur inattendue : {0}";
        public const string MissingScope = "L'import n'indique pas d'année académique ou de semaines.";
    }
}
