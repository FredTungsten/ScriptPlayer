using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScriptPlayer.Shared;
using ScriptPlayer.Shared.Classes.Wrappers;
using ScriptPlayer.Shared.Scripts;
using ScriptPlayer.ViewModels;

namespace ScriptPlayer.Generators
{
    public class HeatmapGenerator : FfmpegGenerator<HeatmapGeneratorSettings>
    {
        private bool _canceled;
        private FfmpegWrapper _wrapper;
        
        public HeatmapGenerator(MainViewModel viewModel) : base(viewModel)
        {
            
        }

        protected override string ProcessingType => "Heatmap";

        protected override GeneratorResult ProcessInternal(HeatmapGeneratorSettings settings, GeneratorEntry entry)
        {
            try
            {
                entry.State = JobStates.Processing;

                _wrapper = new FfmpegWrapper(FfmpegExePath);

                var videoInfo = _wrapper.GetVideoInfo(settings.VideoFile);

                if (videoInfo.Duration <= TimeSpan.Zero)
                {
                    entry.State = JobStates.Done;
                    entry.DoneType = JobDoneTypes.Failure;
                    entry.Update("Failed", 1);
                    return GeneratorResult.Failed();
                }

                TimeSpan duration = videoInfo.Duration;

                //TODO

                string script = ViewModel.GetScriptFile(settings.VideoFile);
                var actions = ViewModel.LoadScriptActions(script, null);

                if(actions == null || actions.Count == 0)
                {
                    entry.State = JobStates.Done;
                    entry.DoneType = JobDoneTypes.Failure;
                    entry.Update("Failed", 1);
                    return GeneratorResult.Failed();
                }

                List<TimedPosition> timeStamps = ViewModel.FilterDuplicates(actions.ToList()).Cast<FunScriptAction>().Select(f => new
                    TimedPosition
                    {
                        Position = f.Position,
                        TimeStamp = f.TimeStamp
                    }).ToList();

                Brush heatmap = HeatMapGenerator.Generate3(timeStamps, TimeSpan.FromSeconds(10), TimeSpan.Zero, duration, 1.0, out Geometry bounds);
                bounds.Transform = new ScaleTransform(settings.Width, settings.Height);
                var rect = new Rect(0, 0, settings.Width, settings.Height);

                DrawingVisual visual = new DrawingVisual();
                using (DrawingContext context = visual.RenderOpen())
                {
                    if (!settings.TransparentBackground)
                    {
                        context.DrawRectangle(Brushes.Black, null, rect);
                    }

                    if (settings.MovementRange)
                    {
                        context.PushClip(bounds);
                    }

                    context.DrawRectangle(heatmap, null, rect);
                    
                    if (settings.AddShadow)
                    {
                        LinearGradientBrush shadow = new LinearGradientBrush();
                        shadow.StartPoint = new Point(0.5,0);
                        shadow.EndPoint = new Point(0.5,1);

                        shadow.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF - 0x20, 0, 0, 0), 0));
                        shadow.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF - 0xcc, 0, 0, 0), 0.98));
                        shadow.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF - 0x50, 0, 0, 0), 0.98));
                        shadow.GradientStops.Add(new GradientStop(Color.FromArgb(0xFF - 0x50, 0, 0, 0), 1));

                        context.DrawRectangle(shadow, null, rect);
                    }

                    if (settings.MovementRange)
                    {
                        context.Pop();
                    }
                }

                RenderTargetBitmap bitmap = new RenderTargetBitmap(settings.Width, settings.Height, 96, 96, PixelFormats.Pbgra32);
                bitmap.Render(visual);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));

                using(FileStream stream = new FileStream(settings.OutputFile, FileMode.Create))
                    encoder.Save(stream);

                entry.DoneType = JobDoneTypes.Success;
                entry.Update("Done", 1);

                return GeneratorResult.Succeeded(settings.OutputFile);
            }
            catch (Exception)
            {
                entry.Update("Failed", 1);
                entry.DoneType = JobDoneTypes.Failure;

                return GeneratorResult.Failed();
            }
            finally
            {
                entry.State = JobStates.Done;

                if (_canceled)
                {
                    entry.DoneType = JobDoneTypes.Cancelled;
                    entry.Update("Cancelled", 1);
                }
            }
        }

        public override void Cancel()
        {
            _canceled = true;
            _wrapper?.Cancel();
        }
    }
}
