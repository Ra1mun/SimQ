using System;

namespace SimQCore.Library.Distributions {
    /// <summary>
    /// Класс Биномиального распределения с параметрами p и n
    /// </summary>
    public class BinomialDistribution: IDistribution {
        private BaseSensor _baseSensor;
        private double p;
        private int n;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="p">Вероятность «успеха»</param>
        /// <param name="n">Число «испытаний»</param>
        public BinomialDistribution( double p, int n ) {
            _baseSensor = new BaseSensor();
            this.p = p;
            this.n = n;
        }
        public double Generate() {
            double x = -1, sum = 0;
            do {
                var randValue = _baseSensor.Next();
                sum += Math.Floor( Math.Log( randValue ) / Math.Log( 1 - p ) ) + 1.0;
                ++x;
            } while( sum <= n );
            return x;
        }
    }
}