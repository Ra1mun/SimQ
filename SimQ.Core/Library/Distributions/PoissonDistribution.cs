using System;

namespace SimQCore.Library.Distributions {
    /// <summary>
    /// Класс распределение Пуассона с параметром rate
    /// </summary>
    public class PoissonDistribution: IDistribution {
        private BaseSensor _baseSensor;
        private double expRateInv;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="rate">Среднее количество успешных испытаний в заданной области возможных исходов</param>
        public PoissonDistribution( double rate ) {
            _baseSensor = new BaseSensor();
            expRateInv = Math.Exp( -rate );
        }

        public double Generate() {
            double k = 0, prod = _baseSensor.Next();
            while( prod > expRateInv ) {
                prod *= _baseSensor.Next();
                ;
                ++k;
            }
            return k;
        }

        public double Generate( double rate ) {
            var expRateInv = Math.Exp(-rate);
            double k = 0, prod = _baseSensor.Next();
            while( prod > expRateInv ) {
                prod *= _baseSensor.Next();
                ;
                ++k;
            }
            return k;
        }
    }
}