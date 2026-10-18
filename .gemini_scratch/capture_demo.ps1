Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Reflection.Assembly]::LoadFrom("C:\Users\0x2b\Documents\projects\TextBox\STTextBox\STTextBox\bin\Debug\STTextBoxTest.exe")
[System.Windows.Forms.Application]::EnableVisualStyles()
$form = New-Object STTextBoxTest.Demos.FrmQuickSelectionDemo
$form.Show()
$form.Refresh()
[System.Windows.Forms.Application]::DoEvents()
[System.Threading.Thread]::Sleep(300)
[System.Windows.Forms.Application]::DoEvents()

$bmp = New-Object System.Drawing.Bitmap($form.Width, $form.Height)
$form.DrawToBitmap($bmp, (New-Object System.Drawing.Rectangle(0, 0, $form.Width, $form.Height)))
$bmp.Save("C:\Users\0x2b\.gemini\antigravity-ide\brain\69fa8706-276e-439d-b5c0-3c0f6a9cfa88\scratch\demo_proportional_segoe.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
$form.Close()
Write-Host "Demo screenshot captured successfully."
