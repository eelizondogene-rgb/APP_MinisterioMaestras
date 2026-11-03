function closeMobileMenu(){
  const wasOpen=$('.sidebar').classList.contains('open');
  $('.sidebar').classList.remove('open');$('#menuBackdrop').classList.add('hidden');
  document.body.classList.remove('menu-open');$('#menuBtn').setAttribute('aria-expanded','false');
  $('.sidebar').inert=innerWidth<=850;
  if(wasOpen)$('#menuBtn').focus({preventScroll:true});
}
function openMobileMenu(){
  $('.sidebar').inert=false;
  $('.sidebar').classList.add('open');$('#menuBackdrop').classList.remove('hidden');
  document.body.classList.add('menu-open');$('#menuBtn').setAttribute('aria-expanded','true');$('#closeMenu').focus();
}
$('#closeMenu').onclick=closeMobileMenu;$('#menuBackdrop').onclick=closeMobileMenu;
document.addEventListener('keydown',event=>{
  if(!$('.sidebar').classList.contains('open'))return;
  if(event.key==='Escape')closeMobileMenu();
  if(event.key==='Tab'){
    const items=[...$('.sidebar').querySelectorAll('button,a,input,select')].filter(e=>e.getClientRects().length&&!e.disabled);
    const first=items[0],last=items.at(-1);
    if(event.shiftKey&&document.activeElement===first){event.preventDefault();last.focus();}
    else if(!event.shiftKey&&document.activeElement===last){event.preventDefault();first.focus();}
  }
});
window.addEventListener('resize',()=>{if(innerWidth>850)closeMobileMenu();else if(!$('.sidebar').classList.contains('open'))$('.sidebar').inert=true;});
closeMobileMenu();
// Preserve semantic tables on desktop; label each value for stacked mobile cards.
function labelMobileTables(){
  for(const table of document.querySelectorAll('#content table')){
    const headers=[...table.querySelectorAll('thead th')].map(h=>h.textContent.trim());
    for(const row of table.querySelectorAll('tbody tr'))[...row.children].forEach((cell,i)=>{
      cell.dataset.label=headers[i]||'Acciones';
      for(const input of cell.querySelectorAll('input,select,textarea'))if(!input.getAttribute('aria-label'))input.setAttribute('aria-label',headers[i]||'Valor');
    });
    table.classList.add('mobile-cards');
  }
}
new MutationObserver(labelMobileTables).observe($('#content'),{childList:true,subtree:true});labelMobileTables();
