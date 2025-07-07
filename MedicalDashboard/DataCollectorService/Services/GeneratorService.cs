namespace DataCollectorService.Services
{
    public class GeneratorService : IGeneratorService
    {
        private readonly Random _random = new();
        private readonly ILogger<GeneratorService> _logger;
        private const double NormalHeartRate = 75.0;
        private const double NormalSaturation = 97.5;
        private const double NormalTemperature = 36.6;
        private const double NormalRespiration = 18.0;
        private const double NormalHemoglobin = 140.0;
        private const double NormalCholesterol = 4.5;
        public GeneratorService(ILogger<GeneratorService> logger)
        {
            _logger = logger;
        }

        public double GeneratePulse(double? previous)
        {
            try
            {
                double baseValue;
                if (previous.HasValue)
                {
                    double correction = (NormalHeartRate - previous.Value) * 0.1;
                    baseValue = previous.Value + _random.Next(-2, 3) * correction;
                }
                else
                {
                    baseValue = _random.Next(65, 80);
                }
                baseValue = Math.Clamp(baseValue, 50, 150);
                return _random.NextDouble() > 0.1  // 10% вероятность аномалии 
                    ? baseValue
                    : _random.NextDouble() > 0.5
                        ? _random.Next(40, 50)   // Брадикардия
                        : _random.Next(130, 151); // Тахикардия
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка считывания пульса");
                return previous ?? NormalHeartRate;
            }

        }

        public double GenerateSaturation(double? previous)
        {
            try
            {
                double baseValue;
                if (previous.HasValue)
                {
                    double correction = (NormalSaturation - previous.Value) * 0.1;
                    baseValue = previous.Value + _random.NextDouble() * correction;
                }
                else
                {
                    baseValue = _random.Next(96, 100);
                }
                baseValue = Math.Clamp(previous.Value + _random.NextDouble() - 0.5, 80, 100);  // Плавное изменение (+-0.5%)

                return _random.NextDouble() > 0.05  // 5% вероятность кризиса
                    ? Math.Round(baseValue, 1)
                    : Math.Round(_random.Next(80, 95) + _random.NextDouble(), 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка считывания сатурации");
                return previous ?? NormalSaturation;
            }
        }

        public double GenerateSystolicPressure()
        {
            try
            {
                if (_random.NextDouble() > 0.07)
                {
                    return (
                        _random.Next(110, 130) // Норма: 110-129
                    );
                }
                else
                {
                    return (_random.Next(75, 109));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка считывания систолического давления");
                return 120;
            }
        }

        public double GenerateDiastolicPressure()
        {
            try
            {
                if (_random.NextDouble() > 0.07)
                {
                    return (
                        _random.Next(75, 109) // Норма: 70-84
                    );
                }
                else
                {
                    return (_random.Next(45, 74));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка считывания диастолического давления");
                return 120;
            }
        }

        public double GenerateWeight(double? previous, double baseWeight)
        {
            try
            {
                double baseValue;
                if (previous == null || previous == 0)
                {
                    baseValue = baseWeight;
                }
                else
                {
                    baseValue = previous.Value;
                }
                if (DateTime.Now.Hour > 18)
                {
                    baseValue += 0.5;  // Вечером вес выше
                }

                double fluctuation = (_random.NextDouble() - 0.5) * 0.4;  // Нормальные колебания (+-0.2 кг)
                double newValue = baseValue + fluctuation;
                if (_random.NextDouble() < 0.02) // 2% вероятность значительного изменения
                {
                    newValue = _random.NextDouble() > 0.5
                        ? baseValue - _random.Next(3, 6) // Потеря
                        : baseValue + _random.Next(2, 5); // Набор
                }
                return Math.Round(Math.Clamp(newValue, baseWeight * 0.7, baseWeight * 1.5), 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка считывания веса");
                return previous ?? baseWeight;
            }
        }

        public double GenerateBMI(double? previous, double baseWeight, double height)
        {
            double baseBMI = baseWeight / Math.Pow(height, 2);
            try
            {
                double currentWeight = GenerateWeight(previous, baseWeight);
                double bmi = currentWeight / Math.Pow(height, 2);
                double baseValue = previous ?? bmi;
                return bmi;
            }
            catch (Exception ex) 
            {
                _logger.LogError(ex, "Ошибка считывания индекса массы тела");
                return previous ?? baseBMI;
            }
        }

        public double GenerateTemperature(double? previous)
        {
            try
            {
                double baseValue;
                if (previous.HasValue)
                {
                    double correction = (NormalTemperature - previous.Value) * 0.3;
                    baseValue = previous.Value + (_random.NextDouble() * 0.2 - 0.1) + correction;
                }
                else
                {
                    baseValue = 36.4 + _random.NextDouble() * 0.8; // 36.4 - 37.2
                }
                if (previous.HasValue && _random.NextDouble() < 0.05)
                {
                    double anomalyValue = _random.NextDouble() > 0.5
                        ? 37.5 + _random.NextDouble() * 2.0 // Лихорадка: 37.5-39.5
                        : 35.0 - _random.NextDouble();      // Гипотермия: 34.0-35.0
                    return Math.Round(Math.Clamp(anomalyValue, 34, 42), 1);
                }
                return Math.Round(Math.Clamp(baseValue, 34, 42), 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка считывания температуры");
                return previous ?? NormalTemperature;
            }
        }

        public double GenerateRespiration(double? previous)
        {
            try
            {
                double baseValue = previous.HasValue
                    ? Math.Clamp(previous.Value + _random.Next(-1, 2), 8, 40)
                    : _random.Next(16, 21); // Норма 16-20 вдохов в минуту

                return _random.NextDouble() < 0.05
                    ? _random.NextDouble() > 0.5
                        ? _random.Next(30, 41) // Тахипноэ
                        : _random.Next(8, 13)  // Брадипноэ
                    : baseValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка считывания частоты дыхания");
                return previous ?? 18;
            }
        }

        public double GenerateHemoglobin(double? previous)
        {
            try
            {
                double baseValue = previous.HasValue
                    ? Math.Clamp(previous.Value + _random.Next(-1, 2), 70, 200)
                    : _random.Next(120, 151);

                return _random.NextDouble() < 0.03
                    ? _random.Next(70, 100)
                    : baseValue;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка генерации гемоглобина");
                return previous ?? 140;
            }
        }

        public double GenerateCholesterol(double? previous)
        {
            try
            {
                double baseValue = previous.HasValue
                    ? previous.Value + _random.NextDouble() * 0.2 - 0.1
                    : 3.5 + _random.NextDouble() * 2.7;  // Норма: 3.5 - 6.2 моль/л - первое измерение
                                                         // в последующих - более плавные изменения (+- 0.1)

                var hour = DateTime.UtcNow.Hour;
                if (hour > 20 || hour < 6)  // Небольшое повышение холестерина ночью - норма
                {
                    baseValue *= 1.15;
                }

                return _random.NextDouble() < 0.05  // вероятность аномалии
                    ? 6.5 + _random.NextDouble() * 3.5
                    : Math.Round(Math.Clamp(baseValue, 1, 10), 1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка считывания холестерина");
                return previous ?? 4.2;
            }
        }


    }
}
