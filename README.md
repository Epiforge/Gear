![Gear Logo](Gear.jpg) 

<h1>Gear</h1>

General utilities to help with stuff in .NET Development, from Epiforge.

Supports `netstandard1.3`.

![Build](https://img.shields.io/azure-devops/build/epiforge/gear/1.svg?logo=microsoft&logoColor=white)
![Tests](https://img.shields.io/azure-devops/tests/epiforge/gear/1.svg?compact_message=&logo=microsoft&logoColor=white)
![Coverage](https://img.shields.io/azure-devops/coverage/epiforge/gear/1.svg?logo=microsoft&logoColor=white)
<!-- This goofy thing license striked us twice for using our own packages? Useless... [![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2FEpiforge%2FGear.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2FEpiforge%2FGear?ref=badge_shield) -->

- [Libraries](#libraries)
  - [Nifty Stuff](#nifty-stuff)
    - [Components](#components)
    - [Active Expressions](#active-expressions)
    - [Active Query](#active-query)
    - [Parallel](#parallel)
    - [Named Pipes Single-Instance](#named-pipes-single-instance)
- [License](#license)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)

# Libraries

## Nifty Stuff

### Components

[![Gear.Components Nuget](https://img.shields.io/nuget/v/Gear.Components.svg)](https://www.nuget.org/packages/Gear.Components)

Every group of library authors has their ultimate base class / kitchen sink NuGet, and this one is ours.
It definitely needs more unit test coverage, and we're working on that.
Nab it for yourself if you want to do any of the following stuff our way:
- disposal (async too!)
- generating hash codes
- invoking methods, getting default values, and performing comparisons using reflection as quickly as possible
- property change notification
- range observable collections and observable dictionaries
- stringifying exceptions
- synchronized collections and dictionaries (i.e. for binding with UI elements)
- task resolution (i.e. you have an object that just might be `Task<T>`, and if so, you want to await the `T`)

### Active Expressions

[![Gear.ActiveExpressions Nuget](https://img.shields.io/nuget/v/Gear.ActiveExpressions.svg)](https://www.nuget.org/packages/Gear.ActiveExpressions)

This library accepts a `LambdaExpression` and arguments to pass to it, dissects the `LambdaExpression`'s body, and hooks into change notification events for properties (`INotifyPropertyChanged`), collections (`INotifyCollectionChanged`), and dictionaries (`Gear.Components.INotifyDictionaryChanged`).

```csharp
var elizabeth = Employee.GetByName("Elizabeth"); // Employee implements INotifyPropertyChanged
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth); // expr subscribed to elizabeth's PropertyChanged
```

Then, as changes involving any elements of the expression occur, a chain of automatic re-evaluation will get kicked off, possibly causing the active expression's `Value` property to change.

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth); // expr.Value == 9
elizabeth.Name = "Lizzy"; // expr.Value == 5
```

Also, since exceptions may be encountered after an active expression was created due to subsequent element changes, active expressions also have a `Fault` property, which will be set to the exception that was encountered during evaluation.

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth); // expr.Fault is null
elizabeth.Name = null; // expr.Fault is NullReferenceException
```

Active expressions raise property change events of their own, so listen for those (kinda the whole point)!

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth);
expr.PropertyChanged += (sender, e) =>
{
    if (e.PropertyName == "Fault")
    {
        // Whoops
    }
    else if (e.PropertyName == "Value")
    {
        // Do something
    }
};
```

When you dispose of your active expression, it will disconnect from all the events.

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
using (var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth))
{
    // expr subscribed to elizabeth's PropertyChanged
}
// expr unsubcribed from elizabeth's PropertyChanged
```

Active expressions will also try to automatically dispose of disposable objects they create in the course of their evaluation when and where it makes sense.
Use the `ActiveExpressionOptions` class for more direct control over this behavior.

You can use the static property `Optimizer` to specify an optimization method to invoke automatically during the active expression creation process.
We recommend Tuomas Hietanen's [Linq.Expression.Optimizer](https://thorium.github.io/Linq.Expression.Optimizer), the utilization of which would like like so:

```csharp
ActiveExpression.Optimizer = ExpressionOptimizer.tryVisit;

var a = Expression.Parameter(typeof(bool));
var b = Expression.Parameter(typeof(bool));

var lambda = Expression.Lambda<Func<bool, bool, bool>>
(
    Expression.AndAlso
    (
        Expression.Not(a),
        Expression.Not(b)
    ),
    a,
    b
); // lambda explicitly defined as (a, b) => !a && !b

var expr = ActiveExpression.Create<bool>(lambda, false, false);
// optimizer has intervened and defined expr as (a, b) => !(a || b)
// (because Augustus De Morgan said they're essentially the same thing, but this revision involves less steps)
```

### Active Query

[![Gear.ActiveQuery Nuget](https://img.shields.io/nuget/v/Gear.ActiveQuery.svg)](https://www.nuget.org/packages/Gear.ActiveQuery)

This library provides re-implementations of extension methods you know and love from `System.Linq.Enumerable`, but instead of returning `Enumerable<T>`s and simple values, these return `ActiveEnumerable<T>`s, `ActiveDictionary<TKey, TValue>`s, and `ActiveValue<T>`s.
This is because, unlike traditional LINQ extension methods, these extension methods continuously update their results until those results are disposed.

But... what could cause those updates?

* the source is enumerable, implements `INotifyCollectionChanged`, and raises a `CollectionChanged` event
* the source is a dictionary, implements `Gear.Components.INotifyDictionaryChanged<TKey, TValue>`, and raises a `DictionaryChanged` event
* the elements in the enumerable (or the values in the dictionary) implement `INotifyPropertyChanged` and raise a `PropertyChanged` event
* a reference enclosed by a selector or a predicate passed to the extension method implements `INotifyCollectionChanged`, `Gear.Components.INotifyDictionaryChanged<TKey, TValue>`, or `INotifyPropertyChanged` and raises one of their events

That last one might be a little surprising, but this is because all selectors and predicates passed to Active Query extension methods become active expressions (see above).
This means that you will not be able to pass one that the Active Expressions library doesn't support (e.g. a lambda expression that can't be converted to an expression tree or that contains nodes that Active Expressions doesn't deal with).
But, in exchange for this, you get all kinds of notification plumbing that's just handled for you behind the scenes.

Suppose, for example, you're working on an app that displays a list of notes and you want the notes to be shown in descending order of when they were last edited.

```csharp
var notes = new ObservableCollection<Note>();

var orderedNotes = notes.ActiveOrderBy(note => note.LastEdited, isDescending: true);
notesViewControl.ItemsSource = orderedNotes;
```

From then on, as you add `Note`s to the `notes` observable collection, the `ActiveEnumerable<Note>` named `orderedNotes` will be kept ordered so that `notesViewControl` displays them in the preferred order.

Since the `ActiveEnumerable<T>` is automatically subscribing to events for you, you do need to call `Dispose` on it when you don't need it any more.

```csharp
void Page_Unload(object sender, EventArgs e)
{
    orderedNotes.Dispose();
}
```

But, you may ask, what happens if things are a little bit more complicated because of background work?
Suppose...

```csharp
SynchronizedObservableCollection<Note> notes;
ActiveEnumerable<Note> orderedNotes;
Task.Run(() =>
{
    notes = new SynchronizedObservableCollection<Note>();
    orderedNotes = notes.ActiveOrderBy(note => note.LastEdited, isDescending: true);
});
```

Since we called the `Gear.Components.SynchronizedObservableCollection` constructor in the context of a TPL `Task` and without specifying a `SynchronizationContext`, operations performed on it will not be in the context of our UI thread.
Manipulating this collection on a background thread might be desirable, but there will be a big problem if we bind a UI control to it, since non-UI threads shouldn't be messing with UI controls.
For this specific reason, Active Query offers a special extension method that will perform the final operations on an enumerable (or dictionary) using a specific `SynchronizationContext`.

```csharp
var uiContext = SynchronizationContext.Current;
SynchronizedObservableCollection<Note> notes;
ActiveEnumerable<Note> orderedNotes;
ActiveEnumerable<Note> notesForBinding;
Task.Run(() =>
{
    notes = new SynchronizedObservableCollection<Note>();
    orderedNotes = notes.ActiveOrderBy(note => note.LastEdited, isDescending: true);
    notesForBinding = orderedNotes.SwitchContext(uiContext);
});
```

Or, if you call `SwitchContext` without any arguments but when you know you're already running in the UI's context, it will assume you want to switch to that.

```csharp
SynchronizedObservableCollection<Note> notes;
ActiveEnumerable<Note> orderedNotes;
await Task.Run(() =>
{
    notes = new SynchronizedObservableCollection<Note>();
    orderedNotes = notes.ActiveOrderBy(note => note.LastEdited, isDescending: true);
});
var notesForBinding = orderedNotes.SwitchContext();
```

But, keep in mind that no Active Query extension methods mutate the objects for which they are called, which means now you have two things to dispose, and in the right order!

```csharp
void Page_Unload(object sender, EventArgs e)
{
    notesForBinding.Dispose();
    orderedNotes.Dispose();
}
```

Ahh, but what about exceptions?
Well, active expressions expose a `Fault` property and raise `PropertyChanging` and `PropertyChanged` events for it, but... you don't really see those active expressions as an Active Query caller, do ya?
For that reason, Active Query introduces the `INotifyElementFaultChanges` interface, which is implemented by `ActiveEnumerable<T>`, `ActiveDictionary<TKey, TValue>`, and `ActiveValue<T>`.
You may subscribe to its `ElementFaultChanging` and `ElementFaultChanged` events to be notified when an active expression runs into a problem.
You may also call the `GetElementFaults` method at any time to retrieve a list of the elements (or key/value pairs) that have active expressions that are currently faulted and what the exception was in each case.

As with the Active Expressions library, you can use the static property `Optimizer` to specify an optimization method to invoke automatically during the active expression creation process.
However, please note that Active Query also has its own version of this property on the `ActiveQueryOptions` static class.
If you are not using Active Expressions directly, we recommend using Active Query's property instead because the optimizer will be called only once per extension method call in that case, no matter how many elements or key/value pairs are processed by it.
Optimize your optimization, yo.

### Parallel

[![Gear.Parallel Nuget](https://img.shields.io/nuget/v/Gear.Parallel.svg)](https://www.nuget.org/packages/Gear.Parallel)

This is the library where we stuff all our groovy parallel programming utilities.
Unfortunately, we haven't thought of much groovyness to add to Microsoft's already bad-ass [Dataflow](https://www.nuget.org/packages/System.Threading.Tasks.Dataflow/).
Writing some extension methods that make it quicker to use it seemed like the least we could do... so we did it.

### Named Pipes Single-Instance

[![Gear.NamedPipesSingleInstance Nuget](https://img.shields.io/nuget/v/Gear.NamedPipesSingleInstance.svg)](https://www.nuget.org/packages/Gear.NamedPipesSingleInstance)

Been thinking about using the new [WPF](https://github.com/dotnet/wpf) and [WinForms](https://github.com/dotnet/winforms) UI frameworks on top of [.NET Core](https://github.com/dotnet/corefx), huh?
A little upset that [the old-school method of making your app single-instance](http://blogs.microsoft.co.il/arik/2010/05/28/wpf-single-instance-application/) doesn't work in .NET Core since it doesn't have Remoting?
We were, too.
So, we made this NuGet for ourselves and decided to share it.
This lil' guy make it really simple for the first instance and secondary instances of your app to talk to each other using named pipes.

Here's an example of the App code-behind of a WPF app using this:
```csharp
using Gear.NamedPipesSingleInstance;

public partial class App : Application
{
    public static async Task OnUiThreadAsync(Action action)
    {
        if (Current.Dispatcher.CheckAccess())
        {
            action();
            return;
        }
        await Current.Dispatcher.InvokeAsync(action);
    }

    public static Task ShowMainWindowAsync() => OnUiThreadAsync(() =>
    {
        var mainWindow = Current.Windows.OfType<MainWindow>().FirstOrDefault();
        if (mainWindow != null)
        {
            if (!mainWindow.IsVisible)
                mainWindow.Show();
            else if (mainWindow.WindowState == WindowState.Minimized)
                mainWindow.WindowState = WindowState.Normal;
            mainWindow.Activate();
        }
    });

    public App() =>
        singleInstance = new SingleInstance("yourappnamehere", SecondaryInstanceMessageReceivedHandler);

    readonly SingleInstance singleInstance;

    async void Initialize(object state)
    {
        if (!singleInstance.IsFirstInstance)
        {
            await singleInstance.SendMessageAsync("showmainwindow");
            Environment.Exit(0);
        }

        // Go about the business of starting your app (you'll need to use Current.Dispatcher to get back on the UI thread)
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        ThreadPool.QueueUserWorkItem(Initialize);
        base.OnStartup(e);
    }

    async Task SecondaryInstanceMessageReceivedHandler(object message)
    {
        // Don't forget that you will be on a worker pool thread in here (as opposed to the UI thread)
        switch (message)
        {
            case "showmainwindow":
                await ShowMainWindowAsync();
                break;
        }
    }
}
```

# License

[Apache 2.0 License](LICENSE)

# Contributing

[Click here](CONTRIBUTING.md) to learn how to contribute.

# Acknowledgements

Makes use of the glorious [AsyncEx](https://github.com/StephenCleary/AsyncEx) library by Stephen Cleary.
