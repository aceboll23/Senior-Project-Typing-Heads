# Project Proposal: AI Narrative Engine



(Logan's Idea)



# Main Idea

A web application that allows users to interact with AI while maintaining consistency with a custom knowledge base. Users link to a "vault" of reference information—for fiction this would be lore (characters, locations, history, rules of the world). For example, if you wanted to roleplay in the Star Wars universe with accurate details, you'd populate the vault with Star Wars lore. When chatting with the AI, it references the vault to stay consistent with established canon rather than hallucinating contradictory details. 



A second storage location allows the AI to store session-specific information like new characters or events created during play. This is separate so that the "vault" is read only and ai cant possibly remove important info.


The application is built on ASP.NET Core MVC with SQL Server storing user accounts, vault contents, and session history. Could be hosted on Azure App Service with Azure Blob Storage for document uploads and exports. The would be an external AI API like Claude, Copilot, which would power the AI responses. 







--------------------------------------------------------




## Possible Additional Features



* Potentially **algorithmic component?:** semantic search system (maybe using ChromaDB that converts user messages into vector embeddings and matches them against vault documents using cosine similarity—this retrieval logic determines which lore is relevant to each query and how much context to provide the AI.



* An AI behind the scenes that views the user message, previous chat history, looks at the vault and session info, then compiles a prompt to the main ai telling it how to respond, as who, and relevant info. This insures there is not bleed between characters or a loss of story consistency, because it isn't one lone conversation that runs out of context and becomes unstable. It is a precise prompt.
  
* Ability to edit both AI and human responses so that you are altering how the ai will respond later (It is only aware of the edited version).
  
* Local or open weights AI hosted AI through LMStudio? or probably something different if everything is on the cloud. Different AI are finetuned to respond differently. some are created for fictional writing, some for coding, this allows the user to "plug in" whichever AI they want to fit their needs.
  
* Possibly the ability for multiple AI to respond separately in the chat, to have a group conversation with the user.
  









--------------------

### A Separate But Similar Idea:



* Instead of a Fiction/Narrative engine, this could be used to analyze and interact with a businesses Knowledge Base, For example WOU has all sorts of services, servers, apps, sites, databases, and all sorts of people need to interact with them on some level. Professors, Janitors, students, Deans... they all need to get a wou id, or buy a parking ticket, etc, and some of them need more complex access behind the scenes, to know how to query a database to write a report or something. If the knowledge base was given to an AI that users could interact with, then it would be able to answer questions that would be very hard to google or ask other AI like ChatGPT about. 

  This could be done using documentation on an open source project like Blender, or 


(Adler's Idea)

# Main Idea

 * Campus Navigation Application: A website that allows users to search for buildings, rooms, and campus services with a visual model. Will provide directions to locations as well as draw a map and highlight the destination and starting point. Administrators will be allowed to manage service times as well as locations of services. Google maps will be used as an external API to show outdoor navigation while some approximation will be done for the indoor navigation that is created with references to campus maps. The project will start by showcasing the WOU campus with design that allows for simple implementation of future campus maps.

--------------------------------------------------


(Ian's Idea)

# Main Idea

 * Board Game Collection/Group play
My thought was a website that you could upload your board game collection to keep track of what you want. You would also be able to browse board games with links to where you can get the game (start off with amazon potentially partnering with game shops that sell online to give people options). You would also be able to create playgroups with friends so that you can easily see what games everyone has and potentially create a game night in that group where you each select the games that you'd like to play that night so everyone knows what to bring. Users should also be able to review games. Potentially add in an AI that shows you games you may like based on your reviews. Would need a database for users, games that the users own, playgroups. 




































