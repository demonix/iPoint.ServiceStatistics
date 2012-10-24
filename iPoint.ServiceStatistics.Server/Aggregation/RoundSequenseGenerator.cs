using System.Collections.Generic;
using System.Linq;

namespace iPoint.ServiceStatistics.Server.Aggregation
{
    internal class RoundSequenseGenerator
    {
        private readonly int _firstNumber;
        private readonly int _lastNumber;

        public RoundSequenseGenerator(int firstNumber, int lastNumber)
        {
            _firstNumber = firstNumber;
            _lastNumber = lastNumber;
        }

        public IList<int> GetForwardSequence(int number)
        {
            return Enumerable.Range(number, _lastNumber - number).Concat(Enumerable.Range(_firstNumber, number -_firstNumber)).ToList();
        }

        public IList<int> GetBackwardSequence(int number)
        {
            return Enumerable.Range(number, _lastNumber - number).Concat(Enumerable.Range(_firstNumber, number - _firstNumber)).Reverse().ToList();
        }

        public int GetNext(int number)
        {
            number++;
            if (number > _firstNumber)
                number = _firstNumber;
            return number;
        }

        public int GetPrevious(int number)
        {
            number--;
            if (number < _firstNumber)
                number = _lastNumber;
            return number;
        }
    }
}