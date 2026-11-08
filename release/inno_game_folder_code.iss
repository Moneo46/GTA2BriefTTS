[Code]
var
  GameDirPage: TInputDirWizardPage;

function DirHasGta2(const Dir: string): Boolean;
begin
  Result := FileExists(AddBackslash(Dir) + 'GTA2.exe');
end;

function JsonQuotedPath(const Dir: string): string;
var
  S: string;
begin
  S := Dir;
  StringChangeEx(S, '\', '\\', True);
  Result := '"' + S + '"';
end;

procedure TryDefaultGameDir;
var
  Src, Parent: string;
begin
  Src := ExpandConstant('{src}');
  if DirHasGta2(Src) then
    GameDirPage.Values[0] := Src
  else
  begin
    Parent := ExtractFileDir(RemoveBackslashUnlessRoot(Src));
    if DirHasGta2(Parent) then
      GameDirPage.Values[0] := Parent;
  end;
end;

procedure InitializeWizard;
begin
  GameDirPage := CreateInputDirPage(
    wpSelectDir,
    'GTA2 game folder',
    'Where is GTA2 installed?',
    'Choose the folder that contains GTA2.exe (and the data\ folder with .gxt files).' + #13#10 +
    'This path is saved into config.json for GTA2BriefTTS.exe.',
    False,
    'New Folder');
  GameDirPage.Add('');
  GameDirPage.Values[0] := '';
  TryDefaultGameDir;
end;

function NextButtonClick(CurPageID: Integer): Boolean;
var
  Dir: string;
begin
  Result := True;
  if CurPageID = GameDirPage.ID then
  begin
    Dir := GameDirPage.Values[0];
    if (Dir = '') or not DirHasGta2(Dir) then
    begin
      MsgBox(
        'GTA2.exe was not found in the selected folder.' + #13#10 +
        'Pick the directory where the game is installed.',
        mbError,
        MB_OK);
      Result := False;
    end;
  end;
end;

procedure PatchConfigGameRoot(const ConfigPath, GameRoot: string);
var
  Lines: TArrayOfString;
  I: Integer;
  Quoted: string;
begin
  if not LoadStringsFromFile(ConfigPath, Lines) then
    Exit;
  Quoted := JsonQuotedPath(GameRoot);
  for I := 0 to GetArrayLength(Lines) - 1 do
  begin
    if Pos('"game_root"', Lines[I]) > 0 then
    begin
      Lines[I] := '  "game_root": ' + Quoted + ',';
      Break;
    end;
  end;
  SaveStringsToFile(ConfigPath, Lines, False);
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  GameRoot: string;
begin
  if CurStep = ssPostInstall then
  begin
    GameRoot := GameDirPage.Values[0];
    if GameRoot <> '' then
      PatchConfigGameRoot(ExpandConstant('{app}\config.json'), GameRoot);
  end;
end;
