
# python -m pip install --upgrade build twine

source ../../PYTHON/uni/bin/activate
dotnet build
cp bin/Debug/net10.0/PyLibC.dll src/PyLibC/bin/
python -m build
python -m twine check dist/*
python -m pip install -e .
python -c "import PyLibC; print('PyLibC import successful')"
