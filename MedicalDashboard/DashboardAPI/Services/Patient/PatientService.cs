using DashboardAPI.DTOs;
using DashboardAPI.Repositories;
using DashboardAPI.Repositories.Patient;
using Shared;
using Shared.Extensions.Logging;

namespace DashboardAPI.Services.Patient;

public class PatientService : IPatientService
{
    private readonly IPatientRepository _patientRepository;
    private readonly ILogger<PatientService> _logger;

    public PatientService(IPatientRepository patientRepository, ILogger<PatientService> logger)
    {
        _patientRepository = patientRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<PatientDto>> GetAllAsync(string? name = null, int? ward = null, Guid? doctorId = null, int page = 1, int pageSize = 20)
    {
        _logger.LogInfo($"Получение списка пациентов. Фильтры: name={name}, ward={ward}, doctorId={doctorId}, page={page}, pageSize={pageSize}");
        
        try
        {
            var patients = await _patientRepository.GetAllAsync(name, ward, doctorId, page, pageSize);
            _logger.LogSuccess($"Успешно получено {patients.Count()} пациентов");
            return patients.Select(MapToDto);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при получении списка пациентов", ex);
            throw;
        }
    }

    public async Task<PatientDto?> GetByIdAsync(Guid id)
    {
        _logger.LogInfo($"Получение пациента по ID: {id}");
        
        try
        {
            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient != null)
            {
                _logger.LogSuccess($"Пациент с ID {id} найден: {patient.FirstName} {patient.LastName}");
                return MapToDto(patient);
            }
            else
            {
                _logger.LogWarning($"Пациент с ID {id} не найден");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при получении пациента с ID {id}", ex);
            throw;
        }
    }

    public async Task<PatientDto> CreateAsync(ApiPatientDto apiDto)
    {
        _logger.LogInfo($"Создание нового пациента: {apiDto.FirstName} {apiDto.LastName}, врач: {apiDto.DoctorId}");
        
        try
        {
            var patient = new Models.Patient
            {
                PatientId = Guid.NewGuid(),
                FirstName = apiDto.FirstName,
                MiddleName = apiDto.MiddleName,
                LastName = apiDto.LastName,
                DoctorId = apiDto.DoctorId,
                BirthDate = apiDto.BirthDate,
                Sex = apiDto.Sex,
                Height = apiDto.Height,
                Ward = apiDto.Ward
            };

            var createdPatient = await _patientRepository.CreateAsync(patient);
            _logger.LogSuccess($"Пациент успешно создан с ID: {createdPatient.PatientId}");
            return MapToDto(createdPatient);
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при создании пациента {apiDto.FirstName} {apiDto.LastName}", ex);
            throw;
        }
    }

    public async Task<PatientDto> UpdateAsync(Guid id, ApiPatientDto updateDto)
    {
        _logger.LogInfo($"Обновление пациента с ID: {id}, новые данные: {updateDto.FirstName} {updateDto.LastName}");
        
        try
        {
            var existingPatient = await _patientRepository.GetByIdAsync(id);
            if (existingPatient == null)
            {
                _logger.LogWarning($"Попытка обновления несуществующего пациента с ID: {id}");
                throw new ArgumentException($"Пациент с ID {id} не найден");
            }

            existingPatient.FirstName = updateDto.FirstName;
            existingPatient.MiddleName = updateDto.MiddleName;
            existingPatient.LastName = updateDto.LastName;
            existingPatient.DoctorId = updateDto.DoctorId;
            existingPatient.BirthDate = updateDto.BirthDate;
            existingPatient.Sex = updateDto.Sex;
            existingPatient.Height = updateDto.Height;
            existingPatient.Ward = updateDto.Ward;

            var updatedPatient = await _patientRepository.UpdateAsync(existingPatient);
            _logger.LogSuccess($"Пациент с ID {id} успешно обновлен");
            return MapToDto(updatedPatient);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при обновлении пациента с ID {id}", ex);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogInfo($"Удаление пациента с ID: {id}");
        
        try
        {
            await _patientRepository.DeleteAsync(id);
            _logger.LogSuccess($"Пациент с ID {id} успешно удален");
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при удалении пациента с ID {id}", ex);
            throw;
        }
    }

    public async Task<int> GetTotalCountAsync(string? name = null, int? ward = null, Guid? doctorId = null)
    {
        _logger.LogInfo($"Получение общего количества пациентов. Фильтры: name={name}, ward={ward}, doctorId={doctorId}");
        
        try
        {
            var count = await _patientRepository.GetTotalCountAsync(name, ward, doctorId);
            _logger.LogInfo($"Общее количество пациентов: {count}");
            return count;
        }
        catch (Exception ex)
        {
            _logger.LogFailure($"Ошибка при получении общего количества пациентов", ex);
            throw;
        }
    }

    private static PatientDto MapToDto(Models.Patient patient)
    {
        return new PatientDto
        {
            PatientId = patient.PatientId,
            FirstName = patient.FirstName,
            MiddleName = patient.MiddleName,
            LastName = patient.LastName,
            DoctorId = patient.DoctorId,
            BirthDate = patient.BirthDate,
            Sex = patient.Sex,
            Height = patient.Height,
            Ward = patient.Ward
        };
    }
} 