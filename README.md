# ClassImpl
![nuget](https://img.shields.io/nuget/v/ClassImpl.svg)

Like a faking library, but oriented towards dynamic modification at runtime.

To get started simply create a `new Implementer<T>()`, `T` being the type you want to implement. Alternatively, you can use the non-generic version `new Implementer(Type type)`. 

## Unity Package Manager

ClassImpl can be installed via the Unity Package Manager. To do so, add the following to the `dependencies` array in your `manifest.json`:

```json
"com.pipe01.classimpl": "https://github.com/pipe01/classimpl.git#unity"
```
