![Gear Logo](Gear.jpg) 

<h1>Gear</h1>

General utilities to help with stuff in .NET Development, from Epiforge.

Supports `netstandard1.3`.

![Build Status](https://ci.appveyor.com/api/projects/status/3s25e4ldo2ji1ech?svg=true) ![AppVeyor tests](https://img.shields.io/appveyor/tests/BigBadBleuCheese/gear.svg?logo=appveyor) [![Coverage Status](https://coveralls.io/repos/github/Epiforge/Gear/badge.svg?branch=master)](https://coveralls.io/github/Epiforge/Gear?branch=master) [![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2FEpiforge%2FGear.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2FEpiforge%2FGear?ref=badge_shield)

- [Getting Started](#getting-started)
- [License](#license)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)

# Getting Started

Install the NuGet packages for the functionality you need.

| Library  | Description | &nbsp;
| - | - | -
| Gear.ActiveExpressions | Expressions that automatically re-evaluate when changes occur | [![Gear.ActiveExpressions Nuget](https://img.shields.io/nuget/v/Gear.ActiveExpressions.svg)](https://www.nuget.org/packages/Gear.ActiveExpressions)
| Gear.ActiveQuery | LINQ-style queries that automatically update when changes occur | [![Gear.ActiveQuery Nuget](https://img.shields.io/nuget/v/Gear.ActiveQuery.svg)](https://www.nuget.org/packages/Gear.ActiveQuery)
| Gear.Caching | Caching, including expiration and refreshing | [![Gear.Caching Nuget](https://img.shields.io/nuget/v/Gear.Caching.svg)](https://www.nuget.org/packages/Gear.Caching)
| Gear.Components | Basic, common functionality often used by other Gear libraries | [![Gear.Components Nuget](https://img.shields.io/nuget/v/Gear.Components.svg)](https://www.nuget.org/packages/Gear.Components)
| Gear.Parallel | Extension methods that make utilizing [Dataflow](https://www.nuget.org/packages/System.Threading.Tasks.Dataflow/) quicker | [![Gear.Parallel Nuget](https://img.shields.io/nuget/v/Gear.Parallel.svg)](https://www.nuget.org/packages/Gear.Parallel)

# License

[Apache 2.0 License](LICENSE)

# Contributing

[Click here](CONTRIBUTING.md) to learn how to contribute.

# Acknowledgements

Makes use of the glorious [AsyncEx](https://github.com/StephenCleary/AsyncEx) library by Stephen Cleary.
