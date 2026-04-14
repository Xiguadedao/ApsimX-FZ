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
                try
                {
                    if (soilTemperature == null) return null;

                    // 防护：如果在初始Commence阶段soilTemp可能未建立出数组实例，抓取其引发的未初始化报错
                    double[] soilTempArray = soilTemperature.Value;
                    if (soilTempArray == null || soilTempArray.Length == 0) return null;

                    double[] cueArray = new double[soilTempArray.Length];
                    for (int i = 0; i < cueArray.Length; i++)
                    {
                        cueArray[i] = Value(i);
                    }
                    return cueArray;
                }
                catch
                {
                    // 数据尚未就绪，返回null可使 Report 工具自动跳过该第一天或填入 '?' 安全空缺
                    return null;
                }
            }
        }

        /// <summary>
        /// 当 OrganicFlow 调用 .Value(layerIndex) 时，会触发这里
        /// </summary>
        /// <param name="arrayIndex">当前计算的土壤层级 (0: surface, ...)</param>
        /// <returns>计算出的当前层CUE</returns>
        public double Value(int arrayIndex = -1)
        {
            try
            {
                if (arrayIndex >= 0 && soilTemperature != null)
                {
                    double[] soilTempArray = soilTemperature.Value;
                    if (soilTempArray != null && arrayIndex < soilTempArray.Length)
                    {
                        double cue = Intercept + (Slope * soilTempArray[arrayIndex]);

                        // 强制设置一个安全阈值，防止负数或跨出 [0, 1] 区间
                        return Math.Max(0.0, Math.Min(cue, 1.0));
                    }
                }
            }
            catch
            {
                // 静默消化 SoilTemperature 第0天的初始化报错
            }

            // 如果未能正常获取土层 (意外调用或处在未初始化状态)，返回截距默认值
            return Intercept;
        }
    }
}