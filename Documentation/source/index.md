---
title: Homepage
description: This is the homepage/manual for Tiny FFR (C# Tiny Fixed Function Rendering Library).
icon: fontawesome/solid/house
---

# Welcome to MkDocs

| Method      | Description                          |
| ----------: | :----------------------------------- |
| `GET`       | :material-check:     Fetch resource  |
| `PUT`       | :material-check-all: Update resource |
| `DELETE`    | :material-close:     Delete resource |

Tiny FFR blah (1) agadg 
{ .annotate }

1.	test

![Image title](https://dummyimage.com/600x400/eee/aaa)
/// caption
Caption here
///

!!! note blah
	TYestse seog hsdfhog[^1] 

## Commands

* `mkdocs new [dir-name]` - Create a new project.
* `mkdocs serve` - Start the live-reloading docs server.
* `mkdocs build` - Build the documentation site.
* `mkdocs -h` - Print help message and exit.

## Project layout

    mkdocs.yml    # The configuration file.
    docs/
        index.md  # The documentation homepage.
        ...       # Other markdown pages, images and other files.

=== "Tab 1"

	```csharp
	for (var a = 0f; a < 360f; a += 60f) {
		var angle = new Angle(a); // (1)!
		Console.WriteLine(PercentageUtils.ConvertFractionToPercentageString(angle.FullCircleFraction) + " = " + ColorVect.FromHueSaturationLightness(angle, 1f, 0.5f));
	}
	```

	1. Test test test

	```csharp
	for (var a = 0f; a < 360f; a += 60f) {
		var angle = new Angle(a);
		Console.WriteLine(PercentageUtils.ConvertFractionToPercentageString(angle.FullCircleFraction) + " = " + ColorVect.FromHueSaturationLightness(angle, 1f, 0.5f));
	}
	```

=== "Tab 2"

	```csharp
	for (var a = 0f; a < 360f; a += 60f) {
		var angle = new Angle(a);
		Console.WriteLine(PercentageUtils.ConvertFractionToPercentageString(angle.FullCircleFraction) + " = " + ColorVect.FromHueSaturationLightness(angle, 1f, 0.5f));
	}
	```
	```csharp
	for (var a = 0f; a < 360f; a += 60f) {
		var angle = new Angle(a);
		Console.WriteLine(PercentageUtils.ConvertFractionToPercentageString(angle.FullCircleFraction) + " = " + ColorVect.FromHueSaturationLightness(angle, 1f, 0.5f));
	}
	```
	
```csharp
for (var a = 0f; a < 360f; a += 60f) {
	var angle = new Angle(a);
	Console.WriteLine(PercentageUtils.ConvertFractionToPercentageString(angle.FullCircleFraction) + " = " + ColorVect.FromHueSaturationLightness(angle, 1f, 0.5f));
}
```

<div class="grid cards" markdown>

-   :material-clock-fast:{ .lg .middle } __Set up in 5 minutes__

    ---

    Install [`mkdocs-material`](#) with [`pip`](#) and get up
    and running in minutes

    [:octicons-arrow-right-24: Getting started](#)

-   :fontawesome-brands-markdown:{ .lg .middle } __It's just Markdown__

    ---

    Focus on your content and generate a responsive and searchable static site

    [:octicons-arrow-right-24: Reference](#)

-   :material-format-font:{ .lg .middle } __Made to measure__

    ---

    Change the colors, fonts, language, icons, logo and more with a few lines

    [:octicons-arrow-right-24: Customization](#)

-   :material-scale-balance:{ .lg .middle } __Open Source, MIT__

    ---

    Material for MkDocs is licensed under MIT and available on [GitHub]

    [:octicons-arrow-right-24: License](#)

</div>

`Lorem ipsum dolor sit amet`

:   Sed sagittis eleifend rutrum. Donec vitae suscipit est. Nullam tempus
    tellus non sem sollicitudin, quis rutrum leo facilisis.

`#!csharp var angle = new Angle(a);`

:   Aliquam metus eros, pretium sed nulla venenatis, faucibus auctor ex. Proin
    ut eros sed sapien ullamcorper consequat. Nunc ligula ante.

    Duis mollis est eget nibh volutpat, fermentum aliquet dui mollis.
    Nam vulputate tincidunt fringilla.
    Nullam dignissim ultrices urna non auctor.

[^1]: This is a footnote