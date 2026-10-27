from pathlib import Path
stage=Path(__file__).parent
root=Path(r'M:\Nexa')
text=(stage/'MainWindow.ClassSchedule.cs').read_text(encoding='utf-8')
text=text.replace('    private void OpenClassSchedule()', '    internal void ShowWeeklySchedule() => OpenClassSchedule();\n    private void OpenClassSchedule()')
text=text.replace('new Preset { Name = session.Title }', 'new Preset { Name = session.Title, AccentColor = current.Data.AccentColor, PixelColor = current.Data.PixelColor }')
(root/'MainWindow.ClassSchedule.cs').write_text(text,encoding='utf-8')
(root/'WidgetActivity.cs').write_text((stage/'WidgetActivity.cs').read_text(encoding='utf-8'),encoding='utf-8')
for name,old,new in [
    ('Widgets.cs','w.CalculationHistory.Clear(); w.Value','w.CalculationHistory.Clear(); w.IntegrationSnapshots.Clear(); w.Value'),
    ('NexaReliability.cs','            if (w.Kind == "Scripture pins")','            w.IntegrationSnapshots.Clear();\n            if (w.Kind == "Scripture pins")'),
    ('DashboardContinuity.cs','if (data.Widgets.Any(w => w.IntegrationSnapshots','if (data.Widgets == null || data.Widgets.Any(w => w == null || w.IntegrationSnapshots')]:
    p=root/name;text=p.read_text(encoding='utf-8');p.write_text(text.replace(old,new),encoding='utf-8')
for name in ['stage_core.py','stage_history.py']:
    p=stage/name;p.write_text(p.read_text(encoding='utf-8').replace('.read_text()',".read_text(encoding='utf-8')"),encoding='utf-8')
print('Fixed UTF-8 source copying and snapshot clearing.')
