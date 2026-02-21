async (req) => {
    let el = document.querySelector(req.qSelector)    
    req.style.hide.forEach((el) => {
        document.querySelector(el).hidden = true
    })
    el.style.fontSize = req.style.fontSize
    const text = document.querySelectorAll(req.style.addMarginTopTo);
    text.forEach((el) => {
        el.style.marginTop = req.style.marginTop
    })
}