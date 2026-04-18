using APSIM.Core;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Interfaces;
using System;
using System.Collections.Generic;

namespace Models.Functions.SoilFunctions
{
    /// <summary>
    /// Calculates CO2 Efficiency (CUE) as a function of soil temperature using the Arnoldi model.
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

        /// <summary>最大生长速率</summary>
        [Description("mu_max")]
        public double MuMax { get; set; } = 0.170792;

        /// <summary>生长的最适温度</summary>
        [Description("Topt_mu")]
        public double TOptMu { get; set; } = 27.375838;

        /// <summary>生长的温度带宽常数</summary>
        [Description("eps_mu")]
        public double EpsMu { get; set; } = 13.759376;

        /// <summary>呼吸与生长最适温度的差值</summary>
        [Description("dTopt")]
        public double DTOpt { get; set; } = 23.853796;

        /// <summary>呼吸与生长温度带宽常数的差值</summary>
        [Description("deps")]
        public double Deps { get; set; } = -4.544258;

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
                        return CalculateArnoldiCUE(soilTempArray[arrayIndex]);
                    }
                }
            }
            catch
            {
                // 静默消化 SoilTemperature 第0天的初始化报错
            }

            // 如果未能正常获取土层 (意外调用或处在未初始化状态)，假设土温为 20.0 度计算一个安全回退值
            return CalculateArnoldiCUE(20.0);
        }

        /// <summary>
        /// 核心算法：根据 Arnoldi 模型计算给定温度下的 CUE
        /// </summary>
        private double CalculateArnoldiCUE(double tVal)
        {
            double tOptR = TOptMu + DTOpt;
            double epsR = EpsMu + Deps;
            double tLethal = TOptMu + EpsMu;

            // 高温保护：超过 lethal 后直接归零
            if (tVal >= tLethal)
            {
                return 0.0;
            }

            double xMu = (tVal - TOptMu) / EpsMu;
            double xR = (tVal - tOptR) / epsR;

            double rateGrowth = MuMax * Math.Exp(xMu) * (1.0 - xMu);
            double rateResp = Math.Exp(xR) * (1.0 - xR);

            if (rateGrowth <= 0.0)
            {
                return 0.0;
            }

            double denom = rateGrowth + rateResp;

            if (denom <= 1e-12)
            {
                return 0.0;
            }

            return Math.Max(0.0, rateGrowth / denom);
        }
    }
}