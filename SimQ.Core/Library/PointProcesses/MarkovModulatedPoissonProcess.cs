using SimQCore.Library.Distributions;

namespace SimQCore.Library.PointProcesses {
    public class MarkovModulatedPoissonProcess {
        private BaseSensor _baseSensor;
        private int _currentIndex;
        private double[][] _matrixP;
        private double[] _arrayRates;
        private double _lastTime = 0;
        private ExponentialDistribution _exponentialDistribution;
        private double _eta;

        public MarkovModulatedPoissonProcess( double [] [] matrixQ, double [] probabilityPointsOnFirst, double [] arrayRates ) {
            _baseSensor = new BaseSensor();

            var matrixP = new double[matrixQ.Length][];
            for( int i = 0; i < matrixP.Length; i++ ) {
                matrixP [i] = new double [matrixP.Length];
                for( int j = 0; j < matrixP.Length; j++ ) {
                    if( i == j )
                        continue;

                    matrixP [i] [j] = -1 * matrixQ [i] [j] / matrixQ [i] [i];
                }
            }

            var rand = _baseSensor.Next();
            var summP = 0.0;
            for( int i = 0; i < probabilityPointsOnFirst.Length; i++ ) {
                summP += probabilityPointsOnFirst [i];

                if( rand < summP ) {
                    _currentIndex = i;
                    break;
                }
            }

            _matrixP = matrixP;
            _arrayRates = arrayRates;
            _exponentialDistribution = new ExponentialDistribution();
            _eta = _lastTime + _exponentialDistribution.Generate( -1 * _matrixP [_currentIndex] [_currentIndex] );
        }

        public double Generate() {
            var tau = _lastTime + _exponentialDistribution.Generate(_arrayRates[_currentIndex]);
            while( tau >= _eta ) {
                _lastTime = _eta;
                var rndValue = _baseSensor.Next();
                var summP = 0.0;
                for( int i = 0; i < _matrixP [_currentIndex].Length; i++ ) {
                    summP += _matrixP [_currentIndex] [i];

                    if( rndValue < summP ) {
                        _currentIndex = i;
                        break;
                    }
                }
                _eta = _lastTime + _exponentialDistribution.Generate( -1 * _matrixP [_currentIndex] [_currentIndex] );
                tau = _lastTime + _exponentialDistribution.Generate( _arrayRates [_currentIndex] );
            }

            _lastTime = tau;
            return _lastTime;
        }
    }
}
