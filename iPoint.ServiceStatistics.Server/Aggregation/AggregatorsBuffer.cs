using System;
using System.Data;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Aggregation.Experimental;
using EventEvaluationLib;

namespace iPoint.ServiceStatistics.Server.Aggregation
{
    public class AggregatorsBuffer
    {
        private int _timeSliceLengthSeconds = 5*60;
        private int _timeSlicingPeriodSeconds = 60;
        private DateTime _baseTime = DateTime.Now;
        private int _currentPeriod = 0;
        private int _currentAggregatorNumber = 0;
        private RoundSequenseGenerator _roundSequense;
        Aggregator[] _aggregators;
        
        private Subject<AggregatedValue> _aggregatedSubject;
        public IObservable<AggregatedValue> AggregateEvents { get { return _aggregatedSubject.AsObservable(); } }

        public AggregatorsBuffer()
        {
            int aggregatorsCount = (int)Math.Truncate((float)_timeSliceLengthSeconds/_timeSlicingPeriodSeconds) + 1;
            //TODO: double check this. aggregatorsCount can't be zero
            if (aggregatorsCount == 0)
                throw new Exception("aggregatorsCount evaluates to zero");
            _aggregators = new Aggregator[aggregatorsCount];
            _roundSequense = new RoundSequenseGenerator(0, aggregatorsCount-1);
        }



       /* public void Push(LogEvent logEvent)
        {
            DateTime eventTime = DateTime.Now; //logEvent.DateTime - in future
            
            int actualPeriod = (int)Math.Truncate((eventTime - _baseTime).TotalSeconds/_timeSliceLengthSeconds);
            switch (_currentPeriod.CompareTo(actualPeriod))
            {
                case 1:
                    //_currentPeriod is greather than eventTime's period, it's not possible when eventTime = DateTime.Now
                    break;
                case 0: //equal
                    foreach (int index in _roundSequense.GetForwardSequence(_currentAggregatorNumber))
                        _aggregators[index].Push(logEvent);
                    break;
                case -1: //_currentPeriod is less than eventTime's period
                    _currentPeriod = actualPeriod;
                    _aggregatedSubject.OnNext(_aggregators[_currentAggregatorNumber].AggregatedValue);
                    _aggregators[_currentAggregatorNumber].Clear();
                    _currentAggregatorNumber = _roundSequense.GetNext(_currentAggregatorNumber);
                    foreach (int index in _roundSequense.GetForwardSequence(_currentAggregatorNumber))
                        _aggregators[index].Push(logEvent);
                    break;
            }
        }*/
    }
}