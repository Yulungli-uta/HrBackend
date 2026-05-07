namespace WsUtaSystem.Models.Views
{
    public class VwDepartmentWithType
    {
        public int DepartmentID { get; set; }
        public string Code { get; set; }
        public string DepartmentName { get; set; }
        public string? ShortName { get; set; }
        public int? ParentID { get; set; }
        public string? ParentDepartmentName { get; set; }
        public int DepartmentTypeID { get; set; }
        public string DepartmentTypeName { get; set; }
        public string? DepartmentTypeDescription { get; set; }
        public int? DepartmentScopeID { get; set; }
        public string? DepartmentScopeName { get; set; }
        public string? DepartmentScopeDescription { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Location { get; set; }
        public int? DeanDirector { get; set; }
        public string? BudgetCode { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
