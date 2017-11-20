param ( 
    $string, 
    [byte[]]$bytes ,
    $object
    )
begin
{
    [void][reflection.assembly]::LoadWithPartialName("System.Windows.Forms")
    [void][reflection.assembly]::LoadWithPartialName("System.Drawing")
    ## the form
    $form = new-object System.Windows.Forms.Form
    $form.size = new-object System.Drawing.Size 800,800

    ## the Rich text box
    $text = new-object System.Windows.Forms.RichTextBox
    $text.multiline = $true
    $text.dock = "Fill"
    $text.scrollbars = "Both"
    $text.width = 80
    # $text.font = new-object system.drawing.font "Lucida Console",12

    ## Quit button
    $QuitButton = new Windows.Forms.Button
    $QuitButton.Name = "QuitButton"
    $QuitButton.TabIndex = 0
    $QuitButton.Text = "Quit"
    $QuitButton.UseVisualStyleBackColor = $true
    $QuitButton.Add_Click({$form.dispose()})
    $QuitButton.Dock = "Bottom"

    $form.controls.add($text)
    $form.controls.add($QuitButton)
    function loadtext
    {
        try
        {
            if ( $object ) { $form.Text = "Title: " + $object.Title; $string = $object.body }
            if ( $string ) { $bytes = [byte[]]($string.ToCharArray()) }
            $stream = new-object io.memorystream $bytes,$true
            $text.loadfile($stream, "richtext")
            $text.DeselectAll()
            [void]$form.showdialog()
        }
        finally
        {
            $stream.close()
            $stream.dispose()
        }
    }

}
end
{
    loadtext
}
