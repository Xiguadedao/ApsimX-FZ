using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using System;
using System.Collections.Generic;

namespace Models.Functions.SoilFunctions
{
    /// <summary>
    /// Calculates CO2 Efficiency (CUE) as a linear function of soil temperature.
    /// Formula: CUE = Intercept + Slope * SoilTemperature
    /// </summary>
    [Serializable]
    [ViewName("UserInterface.Views.PropertyView")]
    [PresenterName("UserInterface.Presenters.PropertyPresenter")]
    [ValidParent(ParentType = typeof(Soils.Nutrients.Nutrient))]
    public class CO2EfficiencyTemperatureFunction : Model, IFunction
    {
        // 自动拉取土壤温度数据
        [Link]
        private ISoilTemperature soilTemperature = null;

        /// <summary>截距</summary>
        [Description("CUE-T Intercept")]
        public double Intercept { get; set; } = 0.63;

        /// <summary>温度斜率</summary>
        [Description("CUE-T Slope")]
        public double Slope { get; set; } = -0.016;

        /// <summary>
        /// 提供给外部 Report 模块读取的每一层 CUE 的属性
        /// </summary>
        public IReadOnlyList<double> Values
        {
            get
            {
                if (soilTemperature?.Value == null) return null;
                
                double[] cueArray = new double[soilTemperature.Value.Length];
                for (int i = 0; i < cueArray.Length; i++)
                {
                    cueArray[i] = Value(i);
                }
                return cueArray;
            }
        }


        /// <summary>
        /// 当 OrganicFlow 调用 .Value(layerIndex) 时，会触发这里
        /// </summary>
        /// <param name="arrayIndex">当前计算的土壤层级 (0: surface, ...)</param>
        /// <returns>计算出的当前层CUE</returns>
        public double Value(int arrayIndex = -1)
        {
            if (arrayIndex >= 0 && soilTemperature != null)
            {
                // soilTemperature.Value 默认返回当天包含每一层的平均温度 Array
                double[] soilTempArray = soilTemperature.Value;
                if (arrayIndex < soilTempArray.Length)
                {
                    double cue = Intercept + (Slope * soilTempArray[arrayIndex]);

                    // 强制设置一个安全阈值，防止负数或超出1
                    return Math.Max(0.0, Math.Min(cue, 1.0));
                }
            }
            // 如果未能正常获取土层(通常是因为意外调用)，返回整体平局默认值或截距
            return Intercept;
        }
    }
}