rmdir /s /q assimp
rmdir /s /q filament
rmdir /s /q sdl
mkdir assimp
mkdir filament
mkdir sdl

cd assimp
cmake ../../assimp

cd ../filament
cmake ../../filament

cd ../sdl
cmake ../../sdl

