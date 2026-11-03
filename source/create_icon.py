from pathlib import Path
from PIL import Image,ImageDraw,ImageFont
p=Path(__file__).resolve().parent.parent/'assets'
p.mkdir(exist_ok=True)
im=Image.new('RGBA',(1024,1024))
d=ImageDraw.Draw(im)
d.rounded_rectangle((16,16,1008,1008),radius=165,fill='#183B56')
d.text((512,480),'TH',font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',470),anchor='mm',fill='white')
im=im.resize((256,256),Image.Resampling.LANCZOS)
im.save(p/'TH.png')
im.save(p/'TH.ico',sizes=[(v,v) for v in [16,24,32,48,64,128,256]])
