using TimeReady.Api.Dtos;
using TimeReady.Api.Models;

namespace TimeReady.Api.Mapping;

public static class EmployeeMappings
{
    public static EmployeeDto ToDto(this Employee employee) => new(
        employee.Id,
        employee.FullName,
        employee.TimeBalanceHours,
        employee.RemainingVacationDays,
        employee.VacationStartDate,
        employee.ManagerInformed,
        employee.HandoverCompleted);

    public static Employee ToEntity(this EmployeeRequest request)
    {
        var employee = new Employee();
        request.ApplyTo(employee);
        return employee;
    }

    public static void ApplyTo(this EmployeeRequest request, Employee employee)
    {
        employee.FullName = request.FullName.Trim();
        employee.TimeBalanceHours = request.TimeBalanceHours;
        employee.RemainingVacationDays = request.RemainingVacationDays;
        employee.VacationStartDate = request.VacationStartDate;
        employee.ManagerInformed = request.ManagerInformed;
        employee.HandoverCompleted = request.HandoverCompleted;
    }
}
