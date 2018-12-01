![Gear Logo](Gear.jpg) 

<h1>Gear</h1>

General utilities to help with stuff in .NET Development, from Epiforge.

Supports `netstandard1.3`.

![Build Status](https://ci.appveyor.com/api/projects/status/3s25e4ldo2ji1ech?svg=true) [![Coverage Status](https://coveralls.io/repos/github/Epiforge/Gear/badge.svg?branch=master)](https://coveralls.io/github/Epiforge/Gear?branch=master) [![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2FEpiforge%2FGear.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2FEpiforge%2FGear?ref=badge_shield)

- [Getting Started](#getting-started)
- [License](#license)
- [Contributing](#contributing)
- [Acknowledgements](#acknowledgements)

# Getting Started

Install the NuGet packages for the functionality you need.

| Package ID | Description | Testing
| - | - | -
| [Gear.ActiveExpressions](https://www.nuget.org/packages/Gear.ActiveExpressions/) | Expressions that automatically re-evaluate when changes occur | Some
| [Gear.ActiveQuery](https://www.nuget.org/packages/Gear.ActiveQuery/) | LINQ-style queries that automatically update when changes occur | None
| [Gear.Caching](https://www.nuget.org/packages/Gear.Caching/) | Caching, including expiration and refreshing | None
| [Gear.Components](https://www.nuget.org/packages/Gear.Components/) | Basic, common functionality often used by other Gear libraries | Some
| [Gear.Parallel](https://www.nuget.org/packages/Gear.Parallel/) | Extension methods that make utilizing [Dataflow](https://www.nuget.org/packages/System.Threading.Tasks.Dataflow/) quicker | None

# License

[Apache 2.0 License](LICENSE)

# Contributing

[Click here](CONTRIBUTING.md) to learn how to contribute.

# Acknowledgements

Makes use of the glorious [AsyncEx](https://github.com/StephenCleary/AsyncEx) library by Stephen Cleary.
