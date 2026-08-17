---
name: xaml-ui
description: Everything needed to add or change Magitek's settings UI — view layout and StackPanel spacing, SettingsBlock structure, the backing setting property, localized strings in Resources.resx and Resources.zh-CN.resx, and shared UserControls. Use whenever creating or modifying anything under Views/UserControls/, adding a user-configurable option, adding any user-visible text, or when a build fails with "Cannot find the static member".
---

# XAML & UI

Magitek's UI is a settings surface: job views made of `SettingsBlock` groups containing
checkboxes and numeric inputs bound to a job's settings singleton.

Adding one user-facing option touches four places. Missing any of them is the single most
common defect in UI pull requests:

1. The setting property in `Models/`
2. The XAML control in `Views/UserControls/`
3. The English string in `Resources.resx`
4. The Chinese string in `Resources.zh-CN.resx`

---

## 1. The setting property

Job settings live in `Magitek/Models/<Job>/<Job>Settings.cs`.

- Inherit from the appropriate role base (`JsonSettings`, `HealerSettings`, `TankSettings`) and implement `IRoutineSettings`.
- Carry `[AddINotifyPropertyChangedInterface]`. Most role bases supply it — verify rather than assume.
- Decorate every serialized property with **both** `[Setting]` and `[DefaultValue(...)]`. A missing `[Setting]` means the value silently fails to persist.
- Group properties in `#region` blocks (`Combat`, `Buffs`, `Heals`).

```csharp
[Setting]
[DefaultValue(75f)]
public float SomeAbilityHealthPercent { get; set; }
```

Singleton and constructor pattern:

```csharp
public SomeSettings() : base(CharacterSettingsDirectory + "/Magitek/Some/SomeSettings.json") { }
public static SomeSettings Instance { get; set; } = new SomeSettings();
```

**ViewModel exposure** uses `{ get; set; }`, never `{ get; }` — PropertyChanged.Fody cannot inject
change notification into a get-only property, and two-way binding breaks silently:

```csharp
public SageSettings SageSettings { get; set; } = SageSettings.Instance;
```

---

## 2. The view

Job views live in `Magitek/Views/UserControls/<Job>/`.

### Required boilerplate

```xml
<UserControl.DataContext>
    <Binding Source="{x:Static viewModels:BaseSettings.Instance}"/>
</UserControl.DataContext>
<UserControl.Resources>
    <ResourceDictionary Source="/Magitek;component/Styles/Magitek.xaml"/>
</UserControl.Resources>
<ScrollViewer VerticalScrollBarVisibility="Auto">
    <StackPanel Margin="10">
        <controls:SettingsBlock ...>
            <!-- StackPanel content -->
        </controls:SettingsBlock>
    </StackPanel>
</ScrollViewer>
```

`BaseSettings` is the root ViewModel exposing every job's settings, so bindings read
`{Binding WhiteMageSettings.PropertyName, Mode=TwoWay}`.

### Spacing — the rule that matters most

**Always** wrap each setting in a `StackPanel` with `Margin="5"`. **Never** use a Grid for a
settings list. This is what produces correct vertical spacing inside a `SettingsBlock`.

```xml
<controls:SettingsBlock Margin="0,5" Background="{DynamicResource ClassSelectorBackground}">
    <StackPanel>
        <TextBlock Style="{DynamicResource TextBlockSection}" Text="..."/>
        <StackPanel Margin="5">
            <StackPanel Margin="5">
                <CheckBox Content="..." IsChecked="{Binding ...}" Style="{DynamicResource CheckBoxFlat}"/>
            </StackPanel>
            <StackPanel Margin="5" Orientation="Horizontal">
                <CheckBox Content="..." IsChecked="{Binding ...}" Style="{DynamicResource CheckBoxFlat}"/>
                <controls:Numeric MaxValue="100" MinValue="1" Value="{Binding ...}"/>
                <TextBlock Style="{DynamicResource TextBlockDefault}" Text="..."/>
            </StackPanel>
        </StackPanel>
    </StackPanel>
</controls:SettingsBlock>
```

Do not do this:

```xml
<Grid Margin="5">
    <Grid.RowDefinitions>
        <RowDefinition/>
        <RowDefinition/>
    </Grid.RowDefinitions>
    <CheckBox Grid.Row="0" .../>
    <CheckBox Grid.Row="1" .../>
</Grid>
```

**Key rules**

- Every setting item is wrapped in `<StackPanel Margin="5">`.
- Checkbox + numeric pairs use `Orientation="Horizontal"`.
- `SettingsBlock` uses `Margin="0,5"`, or `Margin="0,5,0,0"` for the first item.
- The inner content StackPanel uses `Margin="5"`.
- Use `controls:SettingsBlock` to group related options.

### Grid policy

Grid layouts are legacy technical debt. Use one only when the layout genuinely needs alignment
StackPanels cannot provide, such as multi-column numeric controls. When a Grid is unavoidable:

- Keep `Margin="5"` on the parent container.
- Keep the `CheckBox` / `Numeric` / `TextBlock` styling rules.
- Limit the Grid to the complex row so the rest of the block stays StackPanels.

Existing Grid-heavy files such as `Views/UserControls/WhiteMage/Buffs.xaml` are debt. Refactor
toward StackPanels when you are already editing that block; do not block functional work on it.

---

## 3 & 4. Localized strings

Magitek ships English and Simplified Chinese. **Never hardcode display text.** Reference it as
`{x:Static properties:Resources.ResourceName}` and add the string to **both** files, every time:

- `Magitek/Properties/Resources.resx` (English)
- `Magitek/Properties/Resources.zh-CN.resx` (Chinese)

```xml
<data name="Generic_Resource_Name" xml:space="preserve">
    <value>Display Text</value>
</data>
```

**Naming:** `Generic_` for strings shared across jobs; `[JobName]_Content_` or `[JobName]_Text_`
for job-specific ones. Be descriptive — `Generic_AutoGuard_MarksmansSpite`, not `AutoGuard`.

### The Designer.cs gotcha

`Resources.Designer.cs` regenerates only when the project is opened in Visual Studio. Editing
the `.resx` files from an editor or an agent leaves it stale and the build fails with **"Cannot
find the static member"**. This is expected, not a sign something went wrong.

Add the property by hand, in alphabetical order, following the existing pattern:

```csharp
/// <summary>
///   Looks up a localized string similar to Display Text.
/// </summary>
public static string Generic_Resource_Name {
    get {
        return ResourceManager.GetString("Generic_Resource_Name", resourceCulture);
    }
}
```

---

## Shared components

Shared UI lives in `Magitek/Views/UserControls/Common/`. `PvpUtilities` is the reference
implementation. Build one whenever the same settings block would otherwise appear in more than
one job's view.

**Simple DataContext binding — preferred:**

```xml
<common:PvpUtilities DataContext="{Binding [JobName]Settings}"/>
```

The component then binds directly with `{Binding Pvp_PropertyName, Mode=TwoWay}`.

**DependencyProperty pattern — only when the simple form cannot work:**

```csharp
public static readonly DependencyProperty SettingsProperty =
    DependencyProperty.Register("Settings", typeof(JobSettings), typeof(PvpUtilities), new PropertyMetadata(null));

public JobSettings Settings
{
    get { return (JobSettings)GetValue(SettingsProperty); }
    set { SetValue(SettingsProperty, value); }
}
```

When consolidating duplicated markup: create the shared component, update the job-specific
files to use it, delete the duplicates, and verify spacing still follows the StackPanel pattern.
Check every job that should be using it — a migration that misses one job is invisible in the
diff and only shows up in game.

---

## Configuration rule

If you are typing a number that affects behavior, it almost certainly needs to be a setting
rather than a literal, which means all four steps above. Health thresholds, enemy counts,
resource amounts, and timing windows are all user-configurable. Game constants, spell IDs, and
API-defined values are not.

---

## End-to-end checklist

- [ ] Property added to the settings model with `[Setting]` and `[DefaultValue]`
- [ ] ViewModel exposure uses `{ get; set; }`, not `{ get; }`
- [ ] Control added inside a `SettingsBlock`, wrapped in `<StackPanel Margin="5">`
- [ ] Horizontal StackPanel used for checkbox + numeric pairs
- [ ] No new Grid layout
- [ ] All display text referenced via `properties:Resources.*`, none hardcoded
- [ ] String added to `Resources.resx` (English)
- [ ] String added to `Resources.zh-CN.resx` (Chinese)
- [ ] `Resources.Designer.cs` property added if the build complains
- [ ] New view sets both `DataContext` and the `Magitek.xaml` resource dictionary
- [ ] Build passes

## Reference files

- `Magitek/Views/UserControls/Common/PvpUtilities.xaml` — shared component and spacing reference
- `Magitek/Views/UserControls/Sage/Pvp.xaml` — job view reference
- `Magitek/Models/Roles/JobSettings.cs` — shared settings base
- `Magitek/Properties/Resources.resx`, `Resources.zh-CN.resx`, `Resources.Designer.cs`
