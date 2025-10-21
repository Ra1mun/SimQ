using System;

namespace SimQCore.Library.Distributions {
    public class RayleighDistribution: IDistribution {
        private double _sigma;
        private BaseSensor _baseSensor;
        public RayleighDistribution( double sigma ) {
            _sigma = sigma;
            _baseSensor = new BaseSensor();
        }

        public double Generate() {
            return _sigma * Math.Sqrt( -Math.Log( _baseSensor.Next() ) );
        }
    }
}
