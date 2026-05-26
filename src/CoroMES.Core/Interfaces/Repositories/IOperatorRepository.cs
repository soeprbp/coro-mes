using CoroMES.Core.Entities;

namespace CoroMES.Core.Interfaces.Repositories;

/// <summary>
/// Repository for Operator aggregate root.
/// </summary>
public interface IOperatorRepository : IRepository<Operator>
{
    /// <summary>
    /// Get operator by employee number.
    /// </summary>
    Task<Operator?> GetByEmployeeNumberAsync(string employeeNumber);

    /// <summary>
    /// Get operators by shift.
    /// </summary>
    Task<IEnumerable<Operator>> GetByShiftAsync(int shiftId);

    /// <summary>
    /// Get operators currently on duty.
    /// </summary>
    Task<IEnumerable<Operator>> GetOnDutyAsync(DateTime asOf);

    /// <summary>
    /// Get operator with labor records.
    /// </summary>
    Task<Operator?> GetWithLaborRecordsAsync(int operatorId);
}
