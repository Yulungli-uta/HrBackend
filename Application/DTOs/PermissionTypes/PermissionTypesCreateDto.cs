namespace WsUtaSystem.Application.DTOs.PermissionTypes;
public class PermissionTypesCreateDto
{
    //public class PermissionTypes { get; set; }
    public int TypeId { get; set; }
    public string Name { get; set; }
    public bool DeductsFromVacation { get; set; }
    public bool RequiresApproval { get; set; }
    public int MaxDays { get; set; }
}
