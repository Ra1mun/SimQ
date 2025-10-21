namespace SimQCore.Library.Distributions {
    /// <summary>
    /// Класс геометрического распределения с параметром  p
    /// </summary>
    public class GeometricDistibution: IDistribution {
        private BaseSensor _baseSensor;
        private double p;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="p">Вероятность «успеха»</param>
        public GeometricDistibution( double p ) {
            _baseSensor = new BaseSensor();
            this.p = p;
        }

        public double Generate() {
            int k = 0;
            double sum = p, prod = p, q = 1 - p;
            double U = _baseSensor.Next();
            while( U > sum ) {
                prod *= q;
                sum += prod;
                ++k;
            }
            return k;
        }
    }
}
