# Actualmente tenemos dos pestañas: Historia (base) e Historia (Mod).
En la parte superior al mapa hay, los botones de Zoom + ajustar, el año , y los check de titular, condado, ducado, reino e imperio, además de un texto informativo que indica: vista, prov y títulos.

### Sigue los siguientes pasos

1- Antes de nada lee los ficheros **README**.md, PROJECT_CONTEXT.md y **CHANGELOG**.md para entender el proyecto.

2.1- En el texto informativo que se muestra en pantalla, quitar la vista, solo dejar prov y status.
2.2- Me pasas los ficheros enteros a cambiar y lo pruebo.
2.3- Actualizar versión 1.1.2 con los cambios correctos en los ficheros **README**.md, PROJECT_CONTEXT.md y **CHANGELOG**.md

3.1- Unificar en una sola pestaña los dos Historia y solo debe haber uno llamado Mapa.
3.2- Me pasas los ficheros enteros a cambiar y lo pruebo.
3.3- Actualizar versión 1.1.3 con los cambios correctos en los ficheros **README**.md, PROJECT_CONTEXT.md y **CHANGELOG**.md

4.1- Al lado de la fecha, antes de titular por ejemplo, tiene que estar el offset actual, solo informativo y no editable.
4.2- Me pasas los ficheros enteros a cambiar y lo pruebo.
4.3- Actualizar versión 1.1.4 con los cambios correctos en los ficheros **README**.md, PROJECT_CONTEXT.md y **CHANGELOG**.md

5.1- Entre la fecha y los checks, tiene que haber otros check pero no excluyentes que se llamen Base y Mod.
5.2- Me pasas los ficheros enteros a cambiar y lo pruebo.
5.3- Actualizar versión 1.1.5 con los cambios correctos en los ficheros **README**.md, PROJECT_CONTEXT.md y **CHANGELOG**.md

6.1- Los nuevos check funcionan de la siguiente manera: El check base muestra los datos en el mapa del juego base, el check Mod muestra los datos del juego en relación al mod.
6.2- El check Base muestra solo la información del juego base en el mapa. Para el resto, tierra gris, mar azul.
6.3- El check Mod muestra solo la información del mod en el mapa. Para el resto, tierra gris, mar azul. Aquí hay que tener en cuenta el offset del año (que está guardado en Fechas), para que cuadre la información.
6.4- Si no hay ninguno de los dos checks activos, muestra el mapa general de tierra y mar, el que se carga por defecto.
6.5- Si están los dos checks activos, se muestra información de ambos pero en el siguiente orden: primero la del mod, si no hay datos, se muestra la del juego base. En caso de que no haya ninguno, para tierra será el color gris y mar azul. Aquí para el mod también se tiene que tener en cuenta el offset.
6.6- Me pasas los ficheros enteros a cambiar y lo pruebo.
6.7- Actualizar versión 1.1.6 con los cambios correctos en los ficheros **README**.md, PROJECT_CONTEXT.md y **CHANGELOG**.md

7.1- Los checks de título que sí que son excluyentes entre ellos como ya se hace actualmente, solo se mostrará si hay alguno de los nuevos checks activos. Para mayor claridad no se deben abreviar los bombres, por ejemplo que sea: Titular, Condado, … el substantivo. Por defecto el que está activo es el Titular, siempre tiene que haber alguno activo (si algún check nuevo está activo).
7.2- Me pasas los ficheros enteros a cambiar y lo pruebo.
7.3- Actualizar versión 1.1.7 con los cambios correctos en los ficheros **README**.md, PROJECT_CONTEXT.md y **CHANGELOG**.md