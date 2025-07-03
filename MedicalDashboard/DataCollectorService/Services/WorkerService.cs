using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Services
{
    public class WorkerService : BackgroundService
    {
        private readonly IGeneratorService _generator;
        private readonly ILogger<WorkerService> _logger;
        private readonly List<Patient> _patients = new();

        public WorkerService(
            IGeneratorService generator,
            ILogger<WorkerService> logger)
        {
            _generator = generator;
            _logger = logger;

            // Временно добавила инициализацию пациентов, пока их нет в бд
            _patients.Add(new Patient { Name = "Петрова Анна Михайловна", BaseWeight = 60.0, Height = 1.52 });
            _patients.Add(new Patient { Name = "Фролова Ольга Анатольевна", BaseWeight = 78.0, Height = 1.84 });
        }

        protected override async Task ExecuteAsync(CancellationToken ct)
        {
            _logger.LogInformation("Сервис генерации данных запущен");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    foreach (var patient in _patients)
                    {
                        patient.UpdateCount++;
                        MedicalIndicator newMetrics;
                        if (patient.CurrentMetrics == null)
                        {
                            newMetrics = _generator.Generate(patient, null);
                        }
                        else
                        {
                            newMetrics = new MedicalIndicator
                            {
                                Timestamp = DateTime.UtcNow,
                                HeartRate = patient.CurrentMetrics.HeartRate,
                                Saturation = patient.CurrentMetrics.Saturation,
                                Temperature = patient.CurrentMetrics.Temperature,
                                RespirationRate = patient.CurrentMetrics.RespirationRate,
                                SystolicPressure = patient.CurrentMetrics.SystolicPressure,
                                DiastolicPressure = patient.CurrentMetrics.DiastolicPressure,
                                Hemoglobin = patient.CurrentMetrics.Hemoglobin,
                                Weight = patient.BaseWeight,
                                BMI = patient.CurrentMetrics.BMI,
                                Cholesterol = patient.CurrentMetrics.Cholesterol
                            };

                            newMetrics.HeartRate = _generator.GenerateHeartRate(patient.CurrentMetrics.HeartRate);
                            newMetrics.Saturation = _generator.GenerateSaturation(patient.CurrentMetrics.Saturation);
                            newMetrics.BMI = _generator.GenerateBMI(patient.CurrentMetrics.BMI, patient.BaseWeight, patient.Height);

                            if (patient.UpdateCount % 2 == 0) // Обновление каждую минуту
                            {
                                newMetrics.Temperature = _generator.GenerateTemperature(patient.CurrentMetrics.Temperature);
                                newMetrics.RespirationRate = _generator.GenerateRespiration(patient.CurrentMetrics.RespirationRate);
                                (newMetrics.SystolicPressure, newMetrics.DiastolicPressure) = _generator.GeneratePressure();
                                newMetrics.Hemoglobin = _generator.GenerateHemoglobin(patient.CurrentMetrics.Hemoglobin);
                            }

                            // Обновление каждые 5 минут
                            if (patient.UpdateCount % 10 == 0)
                            {
                                newMetrics.Weight = _generator.GenerateWeight(patient.CurrentMetrics.Weight, patient.BaseWeight);
                                newMetrics.Cholesterol = _generator.GenerateCholesterol(patient.CurrentMetrics.Cholesterol);
                            }
                        }

                        patient.CurrentMetrics = newMetrics;
                        LogMetrics(patient);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка в цикле генерации");
                }
                await Task.Delay(TimeSpan.FromSeconds(30), ct); // 1 итерация = 30 с
            }
        }
        public void LogMetrics(Patient patient)
        {
            var metrics = patient.CurrentMetrics;
            _logger.LogInformation(
                $"[{patient.Name}] \n" +
                $"Пульс: {metrics.HeartRate:F1} \n" +
                $"Сатурация: {metrics.Saturation:F1}%\n" +
                $"Температура: {metrics.Temperature:F1}°C\n" +
                $"Дыхание: {metrics.RespirationRate:F1}\n" +
                $"Давление: {metrics.SystolicPressure:F1}/{metrics.DiastolicPressure:F1}\n" +
                $"Гемоглобин: {metrics.Hemoglobin:F1}\n" +
                $"Вес: {metrics.Weight:F1}кг\n" +
                $"Индекс массы тела: {metrics.BMI:F1}\n" +
                $"Холестерин: {metrics.Cholesterol:F1}ммоль/л\n");
        }
    }
}
