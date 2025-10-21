namespace SimQCore.Library.Distributions {
    /// <summary>
    /// Класс распределения Бернулли
    /// </summary>
    public class BernoulliDistribution: IDistribution {
        private BaseSensor _baseSensor;
        private double p;

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="p">Вероятность «успеха»</param>
        public BernoulliDistribution( double p ) {
            _baseSensor = new BaseSensor();
            this.p = p;
        }

        public double Generate() {
            var randValue = _baseSensor.Next();
            return randValue > p ? 0.0 : 1.0;
        }

        public double Generate( double p ) {
            this.p = p;
            var randValue = _baseSensor.Next();
            return randValue > this.p ? 0.0 : 1.0;
        }
    }
}
