async (req) => {
    let el = document.querySelector(req.qSelector)
    if(req.style.padding){
        el.style.padding = req.style.padding
    }
    req.style.hide.forEach((el) => {
        document.querySelectorAll(el).forEach(ee=>{
            ee.hidden = true
        })
    })
    el.style.fontSize = req.style.fontSize
    el.style.color="white"
    
    const text = document.querySelectorAll(req.style.addMarginTopTo);
    text.forEach((el) => {
        el.style.marginTop = req.style.marginTop
        if(req.style.marginStart){
            el.style.marginLeft=req.style.marginStart
        }
        if(req.style.marginEnd){
            el.style.marginRight=req.style.marginEnd
        }
        if(req.style.marginBottom){
            el.style.marginBottom=req.style.marginBottom
        }
        
    })
}