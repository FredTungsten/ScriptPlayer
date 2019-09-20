using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using ScriptPlayer.Shared;
using ScriptPlayer.VideoSync.Annotations;

namespace ScriptPlayer.VideoSync
{
    public partial class ConditionEditorDialog : Window
    {
        public static readonly DependencyProperty ReanalyseProperty = DependencyProperty.Register(
            "Reanalyse", typeof(ReanalyseType), typeof(ConditionEditorDialog), new PropertyMetadata(default(ReanalyseType)));

        public ReanalyseType Reanalyse
        {
            get { return (ReanalyseType) GetValue(ReanalyseProperty); }
            set { SetValue(ReanalyseProperty, value); }
        }

        public event EventHandler<PixelColorSampleCondition> LiveConditionUpdate;       

        public static readonly DependencyProperty ResultProperty = DependencyProperty.Register(
            "Result", typeof(PixelColorSampleCondition), typeof(ConditionEditorDialog), new PropertyMetadata(default(PixelColorSampleCondition)));

        public PixelColorSampleCondition Result
        {
            get { return (PixelColorSampleCondition) GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }

        public static readonly DependencyProperty Result2Property = DependencyProperty.Register(
            "Result2", typeof(AnalysisParameters), typeof(ConditionEditorDialog), new PropertyMetadata(default(AnalysisParameters)));

        public AnalysisParameters Result2
        {
            get { return (AnalysisParameters) GetValue(Result2Property); }
            set { SetValue(Result2Property, value); }
        }

        public static readonly DependencyProperty ConditionsProperty = DependencyProperty.Register(
            "Conditions", typeof(List<ConditionViewModel>), typeof(ConditionEditorDialog), new PropertyMetadata(default(List<ConditionViewModel>)));

        public List<ConditionViewModel> Conditions
        {
            get { return (List<ConditionViewModel>) GetValue(ConditionsProperty); }
            set { SetValue(ConditionsProperty, value); }
        }

        public ConditionEditorDialog(PixelColorSampleCondition condition, AnalysisParameters parameters)
        {
            Conditions = new List<ConditionViewModel>();
            Conditions.Add(new ConditionViewModel("Red", 0, 255, condition?.Red));
            Conditions.Add(new ConditionViewModel("Green", 0, 255, condition?.Green));
            Conditions.Add(new ConditionViewModel("Blue", 0, 255, condition?.Blue));

            Conditions.Add(new ConditionViewModel("Hue", 0, 360, condition?.Hue));
            Conditions.Add(new ConditionViewModel("Sat", 0, 100, condition?.Saturation));
            Conditions.Add(new ConditionViewModel("Lum", 0, 100, condition?.Luminosity));

            InitializeComponent();

            if (condition != null)
            {
                switch (condition.Type)
                {
                    case ConditionType.Absolute:
                        rbAbsolute.IsChecked = true;
                        break;
                    case ConditionType.Relative:
                        rbRelative.IsChecked = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (parameters != null)
            {
                switch (parameters.Method)
                {
                    case TimeStampDeterminationMethod.FirstOccurence:
                        rbFirst.IsChecked = true;
                        break;
                    case TimeStampDeterminationMethod.Center:
                        rbCenter.IsChecked = true;
                        break;
                    case TimeStampDeterminationMethod.LastOccurence:
                        rbLast.IsChecked = true;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach (var cond in Conditions)
                cond.PropertyChanged += CondOnPropertyChanged;
        }

        private void CondOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            OnLiveConditionUpdate(GetCondition());
        }


        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Result = GetCondition();
            Result2 = GetParameters();

            if (rbEverything.IsChecked == true)
                Reanalyse = ReanalyseType.Everything;
            else if (rbSelection.IsChecked == true)
                Reanalyse = ReanalyseType.Selection;
            else
                Reanalyse = ReanalyseType.Nothing;

            DialogResult = true;
        }

        private AnalysisParameters GetParameters()
        {
            AnalysisParameters result = new AnalysisParameters
            {
                MaxPositiveSamples = int.MaxValue
            };

            if(rbFirst.IsChecked == true)
                result.Method = TimeStampDeterminationMethod.FirstOccurence;
            else if (rbCenter.IsChecked == true)
                result.Method = TimeStampDeterminationMethod.Center;
            else if(rbLast.IsChecked == true)
                result.Method = TimeStampDeterminationMethod.LastOccurence;

            return result;
        }

        private PixelColorSampleCondition GetCondition()
        {
            PixelColorSampleCondition result = new PixelColorSampleCondition
            {
                Red = Conditions[0].ToParameter(),
                Green = Conditions[1].ToParameter(),
                Blue = Conditions[2].ToParameter(),
                Hue = Conditions[3].ToParameter(),
                Saturation = Conditions[4].ToParameter(),
                Luminosity = Conditions[5].ToParameter(),
                Type = rbAbsolute.IsChecked == true ? ConditionType.Absolute : ConditionType.Relative
            };

            return result;
        }

        protected virtual void OnLiveConditionUpdate(PixelColorSampleCondition condition)
        {
            LiveConditionUpdate?.Invoke(this, condition);
        }

        private void rbType_Checked(object sender, RoutedEventArgs e)
        {
            OnLiveConditionUpdate(GetCondition());
        }
    }

    public enum ReanalyseType
    {
        Everything,
        Selection,
        Nothing
    }

    public class ConditionViewModel : INotifyPropertyChanged
    {
        private ConditionState _state;
        private int _maximum;
        private int _minimum;
        private int _upperValue;
        private int _lowerValue;
        private string _label;
        private bool _isDoNotUse = true;
        private bool _isInclude;
        private bool _isExclude;

        public string Label
        {
            get { return _label; }
            set
            {
                if (value == _label) return;
                _label = value;
                OnPropertyChanged();
            }
        }

        public int LowerValue
        {
            get { return _lowerValue; }
            set
            {
                if (value.Equals(_lowerValue)) return;
                _lowerValue = value;
                OnPropertyChanged();
            }
        }

        public int UpperValue
        {
            get { return _upperValue; }
            set
            {
                if (value.Equals(_upperValue)) return;
                _upperValue = value;
                OnPropertyChanged();
            }
        }

        public int Minimum
        {
            get { return _minimum; }
            set
            {
                if (value.Equals(_minimum)) return;
                _minimum = value;
                OnPropertyChanged();
            }
        }

        public int Maximum
        {
            get { return _maximum; }
            set
            {
                if (value.Equals(_maximum)) return;
                _maximum = value;
                OnPropertyChanged();
            }
        }

        public ConditionState State
        {
            get { return _state; }
            set
            {
                if (value == _state) return;
                _state = value;

                switch (_state)
                {
                    case ConditionState.NotUsed:
                    {
                        IsDoNotUse = true;
                        IsInclude = false;
                        IsExclude = false;
                        break;
                    }
                    case ConditionState.Include:
                    {
                        IsDoNotUse = false;
                        IsInclude = true;
                        IsExclude = false;
                        break;
                    }
                    case ConditionState.Exclude:
                    {
                        IsDoNotUse = false;
                        IsInclude = false;
                        IsExclude = true;
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                OnPropertyChanged();
            }
        }

        public bool IsInclude
        {
            get { return _isInclude; }
            set
            {
                if (value == _isInclude) return;
                _isInclude = value;
                if(_isInclude)
                    State = ConditionState.Include;
                OnPropertyChanged();
            }
        }

        public bool IsExclude
        {
            get { return _isExclude; }
            set
            {
                if (value == _isExclude) return;
                _isExclude = value;
                if (_isExclude)
                    State = ConditionState.Exclude;
                OnPropertyChanged();
            }
        }

        public bool IsDoNotUse
        {
            get { return _isDoNotUse; }
            set
            {
                if (value == _isDoNotUse) return;
                _isDoNotUse = value;
                if (_isDoNotUse)
                    State = ConditionState.NotUsed;
                OnPropertyChanged();
            }
        }

        public ConditionViewModel(string label, int min, int max, SampleCondtionParameter parameter)
        {
            Label = label;
            Minimum = min;
            Maximum = max;

            if (parameter == null)
            {
                State = ConditionState.NotUsed;
                LowerValue = min;
                UpperValue = max;
            }
            else
            {
                State = parameter.State;
                LowerValue = parameter.MinValue;
                UpperValue = parameter.MaxValue;
            }
        }

        public SampleCondtionParameter ToParameter()
        {
            return new SampleCondtionParameter
            {
                MaxValue = UpperValue,
                MinValue = LowerValue,
                State = State
            };
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
