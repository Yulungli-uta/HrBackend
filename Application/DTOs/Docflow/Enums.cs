namespace WsUtaSystem.Application.DTOs.Docflow
{
    public static class DocflowVisibility
    {
        public const byte PublicWithinCase = 1;
        public const byte PrivateToUploaderDept = 2;
    }

    public static class DocflowMovementType
    {
        public const string Forward = "FORWARD";
        public const string Return = "RETURN";
    }
}
