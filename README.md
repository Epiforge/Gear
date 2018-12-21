![Gear Logo](Gear.jpg) 

<h1>Gear</h1>

General utilities to help with stuff in .NET Development, from Epiforge.

Supports `netstandard1.3`.

![Build](https://img.shields.io/azure-devops/build/epiforge/gear/1.svg?logo=microsoft&logoColor=white)
![Tests](https://img.shields.io/azure-devops/tests/epiforge/gear/1.svg?compact_message=&logo=microsoft&logoColor=white)
![Coverage](https://img.shields.io/azure-devops/coverage/epiforge/gear/1.svg?logo=microsoft&logoColor=white)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2FEpiforge%2FGear.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2FEpiforge%2FGear?ref=badge_shield)

- [Libraries](#libraries)
  - [Nifty Stuff](#nifty-stuff)
    - [Components](#components)
    - [Active Expressions](#active-expressions)
    - [Parallel](#parallel)
  - [Under Construction](#under-construction)
    - [Active Query](#active-query)
  - [Retired](#retired)
    - [Caching](#caching)
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

This library accepts expressions (including lambdas), dissects them, and hooks into change notification events for properties (`INotifyPropertyChanged`), collections (`INotifyCollectionChanged`), and dictionaries (`Gear.Components.INotifyDictionaryChanged`).

```csharp
var elizabeth = Employee.GetByName("Elizabeth"); // Employee implements INotifyPropertyChanged
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth); // expr subscribed to PropertyChanged on elizabeth
```

Then, as changes involving any elements of the expression occur, a chain of automatic re-evaluation will get kicked off, possibly causing the active expression's `Value` property to change.

```csharp
var elizabeth = Employee.GetByName("Elizabeth");
var expr = ActiveExpression.Create(e => e.Name.Length, elizabeth); // expr.Value == 9
elizabeth.Name = "Lizzy"; // expr.Value == 5
```

Also, since exceptions may be encountered long after an active expression was created, they also have a `Fault` property, which will be set to the exception that was encountered during evaluation.

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
    // expr subscribed to PropertyChanged on elizabeth
}
// expr unsubcribed from PropertyChanged on elizabeth
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

### Parallel

[![Gear.Parallel Nuget](https://img.shields.io/nuget/v/Gear.Parallel.svg)](https://www.nuget.org/packages/Gear.Parallel)

This is the library where we stuff all our groovy parallel programming utilities.
Unfortunately, we haven't thought of much groovyness to add to Microsoft's already bad-ass [Dataflow](https://www.nuget.org/packages/System.Threading.Tasks.Dataflow/).
Writing some extension methods that make it quicker to use it seemed like the least we could do... so we did it.

## Under Construction

### Active Query

[![Gear.ActiveQuery Nuget](https://img.shields.io/nuget/v/Gear.ActiveQuery.svg)](https://www.nuget.org/packages/Gear.ActiveQuery)

Combining the power of Active Expressions and LINQ extension methods. You can let that thought sate your apetite for awesome, but you probably shouldn't install this NuGet yet. It has known bugs, essentially no QA, and definitely zero unit test coverage. Check back again later, though. This is next on deck!

## Retired

### Caching

[![Gear.Caching Nuget](https://img.shields.io/nuget/v/Gear.Caching.svg)](https://www.nuget.org/packages/Gear.Caching)

We made this library for a very specific application and don't really use it any more.
But, it was open sourced and did eventually end up in some other people's stuff, so we still let it live here.
Our advice would be not to write anything new using it.

# License

[Apache 2.0 License](LICENSE)

# Contributing

[Click here](CONTRIBUTING.md) to learn how to contribute.

# Acknowledgements

Makes use of the glorious [AsyncEx](https://github.com/StephenCleary/AsyncEx) library by Stephen Cleary.
