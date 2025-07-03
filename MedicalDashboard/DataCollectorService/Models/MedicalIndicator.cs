using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class MedicalIndicator
    {
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public double HeartRate { get; set; }  // ЧСС (уд/мин)
        public double Saturation { get; set; }  // Сатурация (%)
        public double Temperature { get; set; }  // Температура (°C)
        public double RespirationRate { get; set; }  // ЧДД (вдохов/мин)
        public double SystolicPressure { get; set; }  // Верхнее давление
        public double DiastolicPressure { get; set; }  // Нижнее давление
        public double Hemoglobin { get; set; }  // Гемоглобин (г/л)
        public double Weight { get; set; }  // Вес (кг)
        public double BMI { get; set; } // Индекс массы тела
        public double Cholesterol { get; set; }  // Холестерин (ммоль/л)
    }
}
